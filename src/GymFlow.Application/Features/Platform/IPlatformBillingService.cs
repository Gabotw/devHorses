namespace GymFlow.Application.Features.Platform;

/// <summary>
/// Gestión del billing SaaS por el super-admin: ver los gimnasios con su suscripción y
/// asignar/cambiar/cancelar el plan de cada uno. Opera a través de todos los tenants.
/// </summary>
public interface IPlatformBillingService
{
    Task<IReadOnlyList<TenantBillingDto>> ListTenantsAsync(CancellationToken ct = default);
    Task<TenantBillingDto> GetTenantAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Asigna o cambia el plan del tenant (crea o reemplaza su suscripción vigente).</summary>
    Task<SubscriptionDto> AssignAsync(Guid tenantId, AssignSubscriptionRequest request, CancellationToken ct = default);

    /// <summary>Renueva el período de la suscripción vigente (mismo plan) desde hoy.</summary>
    Task<SubscriptionDto> RenewAsync(Guid tenantId, CancellationToken ct = default);

    Task<SubscriptionDto> CancelAsync(Guid tenantId, CancellationToken ct = default);
}
