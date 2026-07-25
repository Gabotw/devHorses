namespace GymFlow.Domain.Enums;

/// <summary>
/// Estado de la suscripción del gimnasio a la plataforma (billing SaaS).
/// El billing real se activa recién con tracción (Fase 6); en validación
/// los tenants nacen como <see cref="Trial"/> o <see cref="Active"/>.
/// </summary>
public enum TenantSubscriptionStatus
{
    Trial = 1,
    Active = 2,
    PastDue = 3,
    Suspended = 4,
    Cancelled = 5,
}
