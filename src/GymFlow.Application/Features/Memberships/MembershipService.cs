using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Abstractions.Tenancy;
using GymFlow.Application.Abstractions.Time;
using GymFlow.Application.Common;
using GymFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Features.Memberships;

/// <summary>
/// Gestión de membresías: asignación de plan, congelamiento y descongelamiento.
/// Un miembro solo puede tener una membresía vigente (activa o congelada) a la vez;
/// una nueva se crea al renovar tras el vencimiento.
/// </summary>
public sealed class MembershipService(
    IAppDbContext db,
    ITenantProvider tenant,
    IClock clock) : IMembershipService
{
    public async Task<IReadOnlyList<MembershipDto>> ListByMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        return await db.Memberships.AsNoTracking()
            .Where(m => m.MemberId == memberId)
            .OrderByDescending(m => m.StartDate)
            .Select(m => MembershipDto.From(m))
            .ToListAsync(ct);
    }

    public async Task<MembershipDto?> GetCurrentAsync(Guid memberId, CancellationToken ct = default)
    {
        var current = await db.Memberships.AsNoTracking()
            .Where(m => m.MemberId == memberId &&
                        (m.Status == MembershipStatus.Active || m.Status == MembershipStatus.Frozen))
            .OrderByDescending(m => m.EndDate)
            .FirstOrDefaultAsync(ct);

        return current is null ? null : MembershipDto.From(current);
    }

    public async Task<IReadOnlyList<ExpiringMembershipDto>> ListExpiringAsync(int withinDays, CancellationToken ct = default)
    {
        var tenantId = tenant.GetRequiredTenantId();
        var today = await TenantTodayAsync(tenantId, ct);
        withinDays = withinDays is < 0 or > 365 ? 7 : withinDays;
        var limit = today.AddDays(withinDays);

        var rows = await (
            from ms in db.Memberships.AsNoTracking()
            where (ms.Status == MembershipStatus.Active || ms.Status == MembershipStatus.Overdue)
                  && ms.EndDate <= limit
            join mem in db.Members on ms.MemberId equals mem.Id
            join pl in db.MembershipPlans on ms.PlanId equals pl.Id
            where mem.Status == MemberStatus.Active
            orderby ms.EndDate
            select new
            {
                ms.Id,
                ms.MemberId,
                mem.FullName,
                mem.Phone,
                PlanName = pl.Name,
                ms.EndDate,
                ms.Status,
            }).ToListAsync(ct);

        return rows
            .Select(r => new ExpiringMembershipDto(
                r.Id, r.MemberId, r.FullName, r.Phone, r.PlanName, r.EndDate, r.Status,
                r.EndDate.DayNumber - today.DayNumber))
            .ToList();
    }

    public async Task<MembershipDto> CreateAsync(CreateMembershipRequest request, CancellationToken ct = default)
    {
        var tenantId = tenant.GetRequiredTenantId();

        var member = await db.Members.FirstOrDefaultAsync(m => m.Id == request.MemberId, ct)
            ?? throw new NotFoundException("Miembro no encontrado.");

        var plan = await db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == request.PlanId, ct)
            ?? throw new NotFoundException("Plan no encontrado.");

        if (!plan.IsActive)
            throw new ConflictException("No se puede asignar un plan inactivo.");

        var hasCurrent = await db.Memberships.AnyAsync(m =>
            m.MemberId == member.Id &&
            (m.Status == MembershipStatus.Active || m.Status == MembershipStatus.Frozen), ct);
        if (hasCurrent)
            throw new ConflictException("El miembro ya tiene una membresía vigente.");

        var startDate = request.StartDate ?? await TenantTodayAsync(tenantId, ct);

        var membership = new Domain.Entities.Membership(tenantId, member, plan, startDate);
        db.Memberships.Add(membership);
        await db.SaveChangesAsync(ct);
        return MembershipDto.From(membership);
    }

    public async Task<MembershipDto> FreezeAsync(
        Guid membershipId, FreezeMembershipRequest request, CancellationToken ct = default)
    {
        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId, ct)
            ?? throw new NotFoundException("Membresía no encontrada.");

        membership.Freeze(request.From, request.Until);
        await db.SaveChangesAsync(ct);
        return MembershipDto.From(membership);
    }

    public async Task<MembershipDto> UnfreezeAsync(
        Guid membershipId, UnfreezeMembershipRequest request, CancellationToken ct = default)
    {
        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId, ct)
            ?? throw new NotFoundException("Membresía no encontrada.");

        membership.Unfreeze(request.ResumeDate);
        await db.SaveChangesAsync(ct);
        return MembershipDto.From(membership);
    }

    private async Task<DateOnly> TenantTodayAsync(Guid tenantId, CancellationToken ct)
    {
        var timeZoneId = await db.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.TimeZoneId)
            .FirstOrDefaultAsync(ct) ?? "America/Lima";

        return clock.TodayIn(timeZoneId);
    }
}
