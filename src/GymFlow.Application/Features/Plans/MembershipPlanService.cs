using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Abstractions.Tenancy;
using GymFlow.Application.Common;
using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Features.Plans;

public sealed class MembershipPlanService(IAppDbContext db, ITenantProvider tenant) : IMembershipPlanService
{
    public async Task<IReadOnlyList<PlanDto>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        var query = db.MembershipPlans.AsNoTracking();
        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        return await query
            .OrderBy(p => p.Price)
            .Select(p => PlanDto.From(p))
            .ToListAsync(ct);
    }

    public async Task<PlanDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var plan = await db.MembershipPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Plan no encontrado.");
        return PlanDto.From(plan);
    }

    public async Task<PlanDto> CreateAsync(CreatePlanRequest request, CancellationToken ct = default)
    {
        await EnsureNameUniqueAsync(request.Name, excludeId: null, ct);

        var plan = new MembershipPlan(
            tenant.GetRequiredTenantId(),
            request.Name,
            request.Price,
            request.DurationDays,
            request.MonthlyAccesses);

        db.MembershipPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        return PlanDto.From(plan);
    }

    public async Task<PlanDto> UpdateAsync(Guid id, UpdatePlanRequest request, CancellationToken ct = default)
    {
        var plan = await db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Plan no encontrado.");

        await EnsureNameUniqueAsync(request.Name, excludeId: id, ct);

        plan.SetName(request.Name);
        plan.SetPrice(request.Price);
        plan.SetDuration(request.DurationDays);
        plan.SetMonthlyAccesses(request.MonthlyAccesses);

        await db.SaveChangesAsync(ct);
        return PlanDto.From(plan);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var plan = await db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Plan no encontrado.");
        plan.Deactivate();
        await db.SaveChangesAsync(ct);
    }

    public async Task ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var plan = await db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Plan no encontrado.");
        plan.Activate();
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureNameUniqueAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        var normalized = (name ?? string.Empty).Trim().ToLower();
        var exists = await db.MembershipPlans
            .AnyAsync(p => p.Name.ToLower() == normalized && (excludeId == null || p.Id != excludeId), ct);
        if (exists)
            throw new ConflictException($"Ya existe un plan llamado '{name}'.");
    }
}
