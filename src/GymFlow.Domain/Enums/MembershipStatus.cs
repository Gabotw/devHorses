namespace GymFlow.Domain.Enums;

/// <summary>
/// Estado del período de membresía.
/// <see cref="Overdue"/> (morosa) se activa en Fase 2 (pagos); se declara aquí para
/// no migrar el enum después.
/// </summary>
public enum MembershipStatus
{
    Active = 1,
    Frozen = 2,
    Expired = 3,
    Overdue = 4,
}
