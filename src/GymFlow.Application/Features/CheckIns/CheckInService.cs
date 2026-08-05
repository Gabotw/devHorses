using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Abstractions.Realtime;
using GymFlow.Application.Abstractions.Tenancy;
using GymFlow.Application.Abstractions.Time;
using GymFlow.Application.Common;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Features.CheckIns;

/// <summary>
/// Registro de asistencia en recepción. Valida que el miembro esté activo y tenga una
/// membresía vigente hoy; si no, guarda igual el intento como no válido (traza). Tras cada
/// ingreso válido recalcula el aforo del día y lo difunde por <see cref="IOccupancyNotifier"/>.
/// </summary>
public sealed class CheckInService(
    IAppDbContext db,
    ITenantProvider tenant,
    IClock clock,
    IOccupancyNotifier occupancy) : ICheckInService
{
    public async Task<CheckInResultDto> RegisterAsync(RegisterCheckInRequest request, CancellationToken ct = default)
    {
        var member = await db.Members.FirstOrDefaultAsync(m => m.Id == request.MemberId, ct)
            ?? throw new NotFoundException("Miembro no encontrado.");

        return await RegisterForMemberAsync(member, request.Method ?? CheckInMethod.Reception, ct);
    }

    public async Task<CheckInResultDto> RegisterByCodeAsync(RegisterByCodeRequest request, CancellationToken ct = default)
    {
        var code = (request.Code ?? string.Empty).Trim();
        if (code.Length != 4 || !code.All(char.IsDigit))
            throw new ConflictException("El código debe tener 4 dígitos.");

        var member = await db.Members.FirstOrDefaultAsync(m => m.AccessCode == code, ct)
            ?? throw new NotFoundException("No hay ningún miembro con ese código.");

        return await RegisterForMemberAsync(member, CheckInMethod.Reception, ct);
    }

    private async Task<CheckInResultDto> RegisterForMemberAsync(Member member, CheckInMethod method, CancellationToken ct)
    {
        var tenantId = tenant.GetRequiredTenantId();
        var today = await TenantTodayAsync(tenantId, ct);
        var nowUtc = clock.UtcNow;

        var reason = await ValidateAsync(member, today, ct);

        var checkIn = reason is null
            ? CheckIn.Valid(tenantId, member.Id, method, nowUtc, today)
            : CheckIn.Rejected(tenantId, member.Id, method, nowUtc, today, reason);

        db.CheckIns.Add(checkIn);
        await db.SaveChangesAsync(ct);

        var current = await CountValidTodayAsync(today, ct);
        await occupancy.OccupancyChangedAsync(tenantId, current, ct);

        var dto = new CheckInDto(
            checkIn.Id, member.Id, member.FullName, checkIn.Method,
            checkIn.OccurredAtUtc, checkIn.IsValid, checkIn.Reason);
        return new CheckInResultDto(dto, current);
    }

    public async Task<IReadOnlyList<CheckInDto>> ListTodayAsync(CancellationToken ct = default)
    {
        var tenantId = tenant.GetRequiredTenantId();
        var today = await TenantTodayAsync(tenantId, ct);

        return await db.CheckIns.AsNoTracking()
            .Where(c => c.LocalDate == today)
            .OrderByDescending(c => c.OccurredAtUtc)
            .Join(db.Members, c => c.MemberId, m => m.Id,
                (c, m) => new CheckInDto(
                    c.Id, c.MemberId, m.FullName, c.Method, c.OccurredAtUtc, c.IsValid, c.Reason))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CheckInDto>> ListByMemberAsync(
        Guid memberId, int take = 50, CancellationToken ct = default)
    {
        take = take is < 1 or > 200 ? 50 : take;

        return await db.CheckIns.AsNoTracking()
            .Where(c => c.MemberId == memberId)
            .OrderByDescending(c => c.OccurredAtUtc)
            .Take(take)
            .Join(db.Members, c => c.MemberId, m => m.Id,
                (c, m) => new CheckInDto(
                    c.Id, c.MemberId, m.FullName, c.Method, c.OccurredAtUtc, c.IsValid, c.Reason))
            .ToListAsync(ct);
    }

    public async Task<int> GetOccupancyAsync(CancellationToken ct = default)
    {
        var tenantId = tenant.GetRequiredTenantId();
        var today = await TenantTodayAsync(tenantId, ct);
        return await CountValidTodayAsync(today, ct);
    }

    /// <summary>Devuelve null si el ingreso es válido; si no, el motivo del rechazo.</summary>
    private async Task<string?> ValidateAsync(Member member, DateOnly today, CancellationToken ct)
    {
        if (member.Status != MemberStatus.Active)
            return "Miembro inactivo.";

        var hasCurrent = await db.Memberships.AsNoTracking().AnyAsync(m =>
            m.MemberId == member.Id &&
            m.Status == MembershipStatus.Active &&
            m.StartDate <= today && today <= m.EndDate, ct);

        return hasCurrent ? null : "Sin membresía vigente.";
    }

    private Task<int> CountValidTodayAsync(DateOnly today, CancellationToken ct) =>
        db.CheckIns.AsNoTracking().CountAsync(c => c.LocalDate == today && c.IsValid, ct);

    private async Task<DateOnly> TenantTodayAsync(Guid tenantId, CancellationToken ct)
    {
        var timeZoneId = await db.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.TimeZoneId)
            .FirstOrDefaultAsync(ct) ?? "America/Lima";

        return clock.TodayIn(timeZoneId);
    }
}
