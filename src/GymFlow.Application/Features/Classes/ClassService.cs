using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Abstractions.Tenancy;
using GymFlow.Application.Abstractions.Time;
using GymFlow.Application.Common;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Features.Classes;

/// <summary>
/// Clases & reservas (Fase 7). El staff programa sesiones con cupo; los miembros reservan y,
/// al llenarse, entran a lista de espera. Cancelar una reserva con cupo promueve a la más
/// antigua en espera (FIFO por CreatedAtUtc). Reservar exige membresía vigente en la fecha de
/// la clase (misma regla que el check-in). El instante vive en UTC; el día local (zona del
/// tenant) permite listar por día sin recalcular rangos.
/// </summary>
public sealed class ClassService(
    IAppDbContext db,
    ITenantProvider tenant,
    IClock clock) : IClassService
{
    private const int DefaultUpcomingDays = 30;

    // --- Staff ---

    public async Task<ClassSessionDto> CreateSessionAsync(CreateClassSessionRequest request, CancellationToken ct = default)
    {
        var tenantId = tenant.GetRequiredTenantId();
        var tz = await TenantTimeZoneAsync(tenantId, ct);
        var startsAtUtc = DateTime.SpecifyKind(request.StartsAtUtc, DateTimeKind.Utc);
        var localDate = DateOnly.FromDateTime(clock.ToLocalTime(startsAtUtc, tz));

        var session = new ClassSession(
            tenantId, request.Name, request.InstructorName, startsAtUtc, localDate,
            request.DurationMinutes, request.Capacity);

        db.ClassSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return ToDto(session, booked: 0, waitlist: 0);
    }

    public async Task<IReadOnlyList<ClassSessionDto>> ListSessionsAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var tenantId = tenant.GetRequiredTenantId();
        var tz = await TenantTimeZoneAsync(tenantId, ct);
        var today = clock.TodayIn(tz);
        var rangeFrom = from ?? today;
        var rangeTo = to ?? rangeFrom.AddDays(DefaultUpcomingDays);
        if (rangeFrom > rangeTo)
            (rangeFrom, rangeTo) = (rangeTo, rangeFrom);

        var sessions = await db.ClassSessions.AsNoTracking()
            .Where(s => s.LocalDate >= rangeFrom && s.LocalDate <= rangeTo)
            .OrderBy(s => s.StartsAtUtc)
            .ToListAsync(ct);

        var counts = await CountsForAsync(sessions.Select(s => s.Id).ToList(), ct);
        return sessions.Select(s => ToDto(s, counts)).ToList();
    }

    public async Task<ClassSessionDto> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await GetSessionEntityAsync(sessionId, tracked: false, ct);
        var counts = await CountsForAsync([session.Id], ct);
        return ToDto(session, counts);
    }

    public async Task<ClassSessionDto> CancelSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await GetSessionEntityAsync(sessionId, tracked: true, ct);
        session.Cancel();

        var active = await db.ClassReservations
            .Where(r => r.ClassSessionId == sessionId &&
                (r.Status == ClassReservationStatus.Booked || r.Status == ClassReservationStatus.Waitlisted))
            .ToListAsync(ct);
        foreach (var reservation in active)
            reservation.Cancel(clock.UtcNow);

        await db.SaveChangesAsync(ct);
        return ToDto(session, booked: 0, waitlist: 0);
    }

    public async Task<IReadOnlyList<ClassReservationDto>> GetRosterAsync(Guid sessionId, CancellationToken ct = default)
    {
        _ = await GetSessionEntityAsync(sessionId, tracked: false, ct);

        return await db.ClassReservations.AsNoTracking()
            .Where(r => r.ClassSessionId == sessionId)
            .OrderBy(r => r.Status)
            .ThenBy(r => r.CreatedAtUtc)
            .Join(db.Members, r => r.MemberId, m => m.Id,
                (r, m) => new ClassReservationDto(
                    r.Id, r.ClassSessionId, r.MemberId, m.FullName, r.Status, r.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<ClassReservationDto> MarkAttendanceAsync(Guid sessionId, Guid memberId, CancellationToken ct = default)
    {
        _ = await GetSessionEntityAsync(sessionId, tracked: false, ct);

        var reservation = await db.ClassReservations
            .FirstOrDefaultAsync(r => r.ClassSessionId == sessionId && r.MemberId == memberId
                && r.Status == ClassReservationStatus.Booked, ct)
            ?? throw new NotFoundException("El miembro no tiene un cupo confirmado en esta clase.");

        reservation.MarkAttended(clock.UtcNow);
        await db.SaveChangesAsync(ct);

        var name = await db.Members.AsNoTracking()
            .Where(m => m.Id == memberId).Select(m => m.FullName).FirstOrDefaultAsync(ct) ?? "";
        return new ClassReservationDto(
            reservation.Id, sessionId, memberId, name, reservation.Status, reservation.CreatedAtUtc);
    }

    // --- Miembro (app) ---

    public async Task<IReadOnlyList<MemberClassSessionDto>> ListUpcomingForMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        var nowUtc = clock.UtcNow;

        var sessions = await db.ClassSessions.AsNoTracking()
            .Where(s => s.Status == ClassSessionStatus.Scheduled && s.StartsAtUtc >= nowUtc)
            .OrderBy(s => s.StartsAtUtc)
            .ToListAsync(ct);

        var ids = sessions.Select(s => s.Id).ToList();
        var counts = await CountsForAsync(ids, ct);
        var mine = await db.ClassReservations.AsNoTracking()
            .Where(r => ids.Contains(r.ClassSessionId) && r.MemberId == memberId && (
                r.Status == ClassReservationStatus.Booked || r.Status == ClassReservationStatus.Waitlisted))
            .ToDictionaryAsync(r => r.ClassSessionId, r => r.Status, ct);

        return sessions.Select(s => ToMemberDto(
            s, counts.GetValueOrDefault(s.Id).Booked,
            mine.TryGetValue(s.Id, out var st) ? st : null)).ToList();
    }

    public async Task<ReserveResultDto> ReserveAsync(Guid sessionId, Guid memberId, CancellationToken ct = default)
    {
        var tenantId = tenant.GetRequiredTenantId();
        var session = await GetSessionEntityAsync(sessionId, tracked: false, ct);

        if (session.IsCancelled)
            throw new ConflictException("La clase fue cancelada.");
        if (session.StartsAtUtc <= clock.UtcNow)
            throw new ConflictException("La clase ya comenzó; no admite reservas.");

        await ValidateMemberCanReserveAsync(memberId, session.LocalDate, ct);

        var alreadyReserved = await db.ClassReservations.AnyAsync(r =>
            r.ClassSessionId == sessionId && r.MemberId == memberId && (
                r.Status == ClassReservationStatus.Booked || r.Status == ClassReservationStatus.Waitlisted), ct);
        if (alreadyReserved)
            throw new ConflictException("Ya tienes una reserva para esta clase.");

        var booked = await CountBookedAsync(sessionId, ct);
        var reservation = booked < session.Capacity
            ? ClassReservation.Booked(tenantId, sessionId, memberId)
            : ClassReservation.Waitlisted(tenantId, sessionId, memberId);

        db.ClassReservations.Add(reservation);
        await db.SaveChangesAsync(ct);

        var dto = await BuildMemberSessionDtoAsync(session, memberId, ct);
        return new ReserveResultDto(reservation.Status, dto);
    }

    public async Task<MemberClassSessionDto> CancelReservationAsync(Guid sessionId, Guid memberId, CancellationToken ct = default)
    {
        var session = await GetSessionEntityAsync(sessionId, tracked: false, ct);

        var reservation = await db.ClassReservations
            .FirstOrDefaultAsync(r => r.ClassSessionId == sessionId && r.MemberId == memberId && (
                r.Status == ClassReservationStatus.Booked || r.Status == ClassReservationStatus.Waitlisted), ct)
            ?? throw new NotFoundException("No tienes una reserva activa para esta clase.");

        var wasBooked = reservation.Status == ClassReservationStatus.Booked;
        reservation.Cancel(clock.UtcNow);

        // Al liberar un cupo, promueve a la reserva en espera más antigua (FIFO).
        if (wasBooked && !session.IsCancelled)
        {
            var next = await db.ClassReservations
                .Where(r => r.ClassSessionId == sessionId && r.Status == ClassReservationStatus.Waitlisted)
                .OrderBy(r => r.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);
            next?.Promote();
        }

        await db.SaveChangesAsync(ct);
        return await BuildMemberSessionDtoAsync(session, memberId, ct);
    }

    public async Task<IReadOnlyList<MyReservationDto>> ListMyReservationsAsync(Guid memberId, CancellationToken ct = default)
    {
        return await db.ClassReservations.AsNoTracking()
            .Where(r => r.MemberId == memberId)
            .Join(db.ClassSessions, r => r.ClassSessionId, s => s.Id,
                (r, s) => new MyReservationDto(r.Id, s.Id, s.Name, s.StartsAtUtc, s.Status, r.Status))
            .OrderByDescending(x => x.StartsAtUtc)
            .ToListAsync(ct);
    }

    // --- Helpers ---

    private async Task ValidateMemberCanReserveAsync(Guid memberId, DateOnly classDate, CancellationToken ct)
    {
        var member = await db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Id == memberId, ct)
            ?? throw new NotFoundException("Miembro no encontrado.");
        if (member.Status != MemberStatus.Active)
            throw new ConflictException("El miembro no está activo.");

        var hasCurrent = await db.Memberships.AsNoTracking().AnyAsync(m =>
            m.MemberId == memberId &&
            m.Status == MembershipStatus.Active &&
            m.StartDate <= classDate && classDate <= m.EndDate, ct);
        if (!hasCurrent)
            throw new ConflictException("Necesitas una membresía vigente en la fecha de la clase para reservar.");
    }

    private async Task<ClassSession> GetSessionEntityAsync(Guid sessionId, bool tracked, CancellationToken ct)
    {
        var query = db.ClassSessions.AsQueryable();
        if (!tracked) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new NotFoundException("Clase no encontrada.");
    }

    private Task<int> CountBookedAsync(Guid sessionId, CancellationToken ct) =>
        db.ClassReservations.AsNoTracking()
            .CountAsync(r => r.ClassSessionId == sessionId && r.Status == ClassReservationStatus.Booked, ct);

    private async Task<Dictionary<Guid, (int Booked, int Waitlist)>> CountsForAsync(
        IReadOnlyCollection<Guid> sessionIds, CancellationToken ct)
    {
        var result = sessionIds.ToDictionary(id => id, _ => (Booked: 0, Waitlist: 0));
        if (sessionIds.Count == 0)
            return result;

        var rows = await db.ClassReservations.AsNoTracking()
            .Where(r => sessionIds.Contains(r.ClassSessionId) && (
                r.Status == ClassReservationStatus.Booked || r.Status == ClassReservationStatus.Waitlisted))
            .GroupBy(r => new { r.ClassSessionId, r.Status })
            .Select(g => new { g.Key.ClassSessionId, g.Key.Status, Count = g.Count() })
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            var cur = result[row.ClassSessionId];
            if (row.Status == ClassReservationStatus.Booked)
                cur.Booked = row.Count;
            else
                cur.Waitlist = row.Count;
            result[row.ClassSessionId] = cur;
        }
        return result;
    }

    private async Task<MemberClassSessionDto> BuildMemberSessionDtoAsync(ClassSession session, Guid memberId, CancellationToken ct)
    {
        var booked = await CountBookedAsync(session.Id, ct);
        var myStatus = await db.ClassReservations.AsNoTracking()
            .Where(r => r.ClassSessionId == session.Id && r.MemberId == memberId && (
                r.Status == ClassReservationStatus.Booked || r.Status == ClassReservationStatus.Waitlisted))
            .Select(r => (ClassReservationStatus?)r.Status)
            .FirstOrDefaultAsync(ct);
        return ToMemberDto(session, booked, myStatus);
    }

    private async Task<string> TenantTimeZoneAsync(Guid tenantId, CancellationToken ct) =>
        await db.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.TimeZoneId)
            .FirstOrDefaultAsync(ct) ?? "America/Lima";

    private static ClassSessionDto ToDto(ClassSession s, int booked, int waitlist) =>
        new(s.Id, s.Name, s.InstructorName, s.StartsAtUtc, s.EndsAtUtc, s.LocalDate,
            s.DurationMinutes, s.Capacity, s.Status, booked, waitlist, Math.Max(0, s.Capacity - booked));

    private static ClassSessionDto ToDto(ClassSession s, Dictionary<Guid, (int Booked, int Waitlist)> counts)
    {
        var c = counts.GetValueOrDefault(s.Id);
        return ToDto(s, c.Booked, c.Waitlist);
    }

    private static MemberClassSessionDto ToMemberDto(ClassSession s, int booked, ClassReservationStatus? myStatus) =>
        new(s.Id, s.Name, s.InstructorName, s.StartsAtUtc, s.EndsAtUtc, s.DurationMinutes,
            s.Capacity, booked, Math.Max(0, s.Capacity - booked), myStatus);
}
