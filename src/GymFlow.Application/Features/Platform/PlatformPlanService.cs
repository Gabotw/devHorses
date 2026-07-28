using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Common;
using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Features.Platform;

/// <summary>
/// CRUD del catálogo de planes de plataforma. Entidad de nivel plataforma: sin global query
/// filter y sin tenant resuelto (el super-admin opera cross-tenant). Dinero siempre decimal.
/// </summary>
public sealed class PlatformPlanService(IAppDbContext db) : IPlatformPlanService
{
    public async Task<IReadOnlyList<PlatformPlanDto>> ListAsync(bool includeInactive = true, CancellationToken ct = default)
    {
        var query = db.PlatformPlans.AsNoTracking();
        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        return await query
            .OrderBy(p => p.MonthlyPrice)
            .Select(p => PlatformPlanDto.From(p))
            .ToListAsync(ct);
    }

    public async Task<PlatformPlanDto> CreateAsync(UpsertPlatformPlanRequest request, CancellationToken ct = default)
    {
        await EnsureNameFreeAsync(request.Name, excludeId: null, ct);

        var plan = new PlatformPlan(request.Name, request.MonthlyPrice, request.BillingPeriodDays, request.MaxMembers);
        db.PlatformPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        return PlatformPlanDto.From(plan);
    }

    public async Task<PlatformPlanDto> UpdateAsync(Guid id, UpsertPlatformPlanRequest request, CancellationToken ct = default)
    {
        var plan = await GetAsync(id, ct);
        await EnsureNameFreeAsync(request.Name, excludeId: id, ct);

        plan.SetName(request.Name);
        plan.SetMonthlyPrice(request.MonthlyPrice);
        plan.SetBillingPeriodDays(request.BillingPeriodDays);
        plan.SetMaxMembers(request.MaxMembers);

        await db.SaveChangesAsync(ct);
        return PlatformPlanDto.From(plan);
    }

    public async Task<PlatformPlanDto> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var plan = await GetAsync(id, ct);
        if (isActive) plan.Activate(); else plan.Deactivate();
        await db.SaveChangesAsync(ct);
        return PlatformPlanDto.From(plan);
    }

    private async Task<PlatformPlan> GetAsync(Guid id, CancellationToken ct)
        => await db.PlatformPlans.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Plan de plataforma no encontrado.");

    private async Task EnsureNameFreeAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        var normalized = (name ?? string.Empty).Trim();
        var taken = await db.PlatformPlans.AsNoTracking()
            .AnyAsync(p => p.Name == normalized && (excludeId == null || p.Id != excludeId), ct);
        if (taken)
            throw new ConflictException($"Ya existe un plan de plataforma llamado '{normalized}'.");
    }
}
