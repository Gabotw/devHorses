using GymFlow.Domain.Common;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

/// <summary>
/// Suscripción de un gimnasio (tenant) a un <see cref="PlatformPlan"/> (billing SaaS, Fase 6).
///
/// Es de nivel plataforma: tiene un <see cref="TenantId"/> pero NO implementa
/// <see cref="ITenantScoped"/>, porque el super-admin la administra a través de todos los
/// tenants (fuera del global query filter, igual que <see cref="Tenant"/>). Hay a lo sumo
/// una suscripción vigente por tenant.
///
/// Guarda un snapshot del plan (nombre, precio, días de período) al momento de suscribir,
/// porque el catálogo de planes puede cambiar después. El dinero es siempre decimal.
/// </summary>
public class Subscription : Entity
{
    // EF Core
    private Subscription() { }

    private Subscription(Guid tenantId, PlatformPlan plan, DateOnly startDate)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId es obligatorio.", nameof(tenantId));
        ArgumentNullException.ThrowIfNull(plan);

        TenantId = tenantId;
        ApplyPlan(plan, startDate);
        Status = TenantSubscriptionStatus.Active;
    }

    public Guid TenantId { get; private set; }

    public Guid PlatformPlanId { get; private set; }

    /// <summary>Nombre del plan al suscribir (snapshot).</summary>
    public string PlanName { get; private set; } = string.Empty;

    /// <summary>Precio del período cobrado al suscribir (snapshot). Decimal.</summary>
    public decimal PriceAtSubscription { get; private set; }

    /// <summary>Días del período de facturación al suscribir (snapshot).</summary>
    public int BillingPeriodDays { get; private set; }

    public TenantSubscriptionStatus Status { get; private set; }

    public DateOnly CurrentPeriodStart { get; private set; }

    public DateOnly CurrentPeriodEnd { get; private set; }

    public DateTime? CanceledAtUtc { get; private set; }

    /// <summary>Inicia una nueva suscripción para un tenant sobre un plan (queda activa).</summary>
    public static Subscription Start(Guid tenantId, PlatformPlan plan, DateOnly startDate) =>
        new(tenantId, plan, startDate);

    /// <summary>Cambia el plan de la suscripción y reinicia el período desde <paramref name="startDate"/>.</summary>
    public void ChangePlan(PlatformPlan plan, DateOnly startDate)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ApplyPlan(plan, startDate);
        Status = TenantSubscriptionStatus.Active;
        CanceledAtUtc = null;
        Touch();
    }

    /// <summary>Renueva el período (mismo plan) a partir de <paramref name="startDate"/>. Reactiva.</summary>
    public void Renew(DateOnly startDate)
    {
        CurrentPeriodStart = startDate;
        CurrentPeriodEnd = startDate.AddDays(BillingPeriodDays);
        Status = TenantSubscriptionStatus.Active;
        CanceledAtUtc = null;
        Touch();
    }

    /// <summary>
    /// Corte de billing SaaS: una suscripción vigente (activa o trial) cuyo período ya venció
    /// pasa a morosa (PastDue). No toca suspendidas/canceladas ni las ya morosas.
    /// </summary>
    public void MarkPastDue()
    {
        if (Status is not (TenantSubscriptionStatus.Active or TenantSubscriptionStatus.Trial))
            return;
        Status = TenantSubscriptionStatus.PastDue;
        Touch();
    }

    /// <summary>Suspende el servicio (p.ej. tras mora prolongada). Reversible con Renew/ChangePlan.</summary>
    public void Suspend()
    {
        Status = TenantSubscriptionStatus.Suspended;
        Touch();
    }

    /// <summary>Cancela la suscripción (baja del servicio).</summary>
    public void Cancel(DateTime nowUtc)
    {
        Status = TenantSubscriptionStatus.Cancelled;
        CanceledAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        Touch();
    }

    private void ApplyPlan(PlatformPlan plan, DateOnly startDate)
    {
        PlatformPlanId = plan.Id;
        PlanName = plan.Name;
        PriceAtSubscription = plan.MonthlyPrice;
        BillingPeriodDays = plan.BillingPeriodDays;
        CurrentPeriodStart = startDate;
        CurrentPeriodEnd = startDate.AddDays(plan.BillingPeriodDays);
    }
}
