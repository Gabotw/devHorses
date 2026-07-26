namespace GymFlow.Domain.Enums;

/// <summary>
/// Estado de un pago. El efectivo nace <see cref="Completed"/>; un cobro por pasarela
/// nace <see cref="Pending"/> y pasa a <see cref="Completed"/> o <see cref="Failed"/>.
/// </summary>
public enum PaymentStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4,
}
