using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Abstractions.Time;
using GymFlow.Application.Common;
using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Features.Platform;

/// <summary>
/// Gestión del billing SaaS. Corre sin tenant resuelto (el super-admin es cross-tenant), así
/// que ignora los global query filters para contar miembros por tenant y trabaja directo sobre
/// Tenants/Subscriptions (entidades de plataforma, sin filtro). Mantiene sincronizado el
/// estado de suscripción cacheado en el tenant (<see cref="Tenant.SubscriptionStatus"/>), que
/// es lo que decide si el gimnasio está activo. Dinero siempre decimal.
/// </summary>
public sealed class PlatformBillingService(IAppDbContext db, IClock clock) : IPlatformBillingService
{
    public async Task<IReadOnlyList<TenantBillingDto>> ListTenantsAsync(CancellationToken ct = default)
    {
        var tenants = await db.Tenants.AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        var subs = await db.Subscriptions.AsNoTracking().ToListAsync(ct);
        var subByTenant = subs.ToDictionary(s => s.TenantId);
        var memberCounts = await MemberCountsAsync(ct);

        return tenants
            .Select(t => Map(t, subByTenant.GetValueOrDefault(t.Id), memberCounts.GetValueOrDefault(t.Id)))
            .ToList();
    }

    public async Task<TenantBillingDto> GetTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await GetTenantEntityAsync(tenantId, ct);
        var sub = await db.Subscriptions.AsNoTracking().FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);
        var count = await db.Members.IgnoreQueryFilters().CountAsync(m => m.TenantId == tenantId, ct);
        return Map(tenant, sub, count);
    }

    public async Task<SubscriptionDto> AssignAsync(Guid tenantId, AssignSubscriptionRequest request, CancellationToken ct = default)
    {
        var tenant = await GetTenantEntityAsync(tenantId, ct);
        var plan = await db.PlatformPlans.FirstOrDefaultAsync(p => p.Id == request.PlatformPlanId, ct)
            ?? throw new NotFoundException("Plan de plataforma no encontrado.");
        if (!plan.IsActive)
            throw new ConflictException("No se puede suscribir a un plan inactivo.");

        var startDate = request.StartDate ?? clock.TodayIn(tenant.TimeZoneId);
        var sub = await db.Subscriptions.FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

        if (sub is null)
        {
            sub = Subscription.Start(tenantId, plan, startDate);
            db.Subscriptions.Add(sub);
        }
        else
        {
            sub.ChangePlan(plan, startDate);
        }

        tenant.SetSubscriptionStatus(sub.Status);
        await db.SaveChangesAsync(ct);
        return SubscriptionDto.From(sub);
    }

    public async Task<SubscriptionDto> RenewAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await GetTenantEntityAsync(tenantId, ct);
        var sub = await GetSubscriptionEntityAsync(tenantId, ct);

        sub.Renew(clock.TodayIn(tenant.TimeZoneId));
        tenant.SetSubscriptionStatus(sub.Status);
        await db.SaveChangesAsync(ct);
        return SubscriptionDto.From(sub);
    }

    public async Task<SubscriptionDto> CancelAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await GetTenantEntityAsync(tenantId, ct);
        var sub = await GetSubscriptionEntityAsync(tenantId, ct);

        sub.Cancel(clock.UtcNow);
        tenant.SetSubscriptionStatus(sub.Status);
        await db.SaveChangesAsync(ct);
        return SubscriptionDto.From(sub);
    }

    private async Task<Tenant> GetTenantEntityAsync(Guid tenantId, CancellationToken ct)
        => await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new NotFoundException("Gimnasio (tenant) no encontrado.");

    private async Task<Subscription> GetSubscriptionEntityAsync(Guid tenantId, CancellationToken ct)
        => await db.Subscriptions.FirstOrDefaultAsync(s => s.TenantId == tenantId, ct)
            ?? throw new NotFoundException("El gimnasio no tiene una suscripción.");

    private async Task<Dictionary<Guid, int>> MemberCountsAsync(CancellationToken ct)
    {
        var rows = await db.Members.IgnoreQueryFilters()
            .GroupBy(m => m.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.TenantId, r => r.Count);
    }

    private static TenantBillingDto Map(Tenant t, Subscription? sub, int memberCount) =>
        new(t.Id, t.Name, t.Subdomain, t.SubscriptionStatus, memberCount,
            sub is null ? null : SubscriptionDto.From(sub));
}
