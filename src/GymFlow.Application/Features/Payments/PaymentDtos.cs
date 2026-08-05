using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.Features.Payments;

/// <summary>Registro de pago en efectivo (recepción). Nace completado.</summary>
public sealed record RegisterCashPaymentRequest(
    Guid MemberId,
    Guid? MembershipId,
    decimal Amount,
    string? Notes);

public sealed record PaymentDto(
    Guid Id,
    Guid MemberId,
    Guid? MembershipId,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    string? GatewayReference,
    string? FailureReason,
    DateTime? PaidAtUtc,
    string? Notes,
    DateTime CreatedAtUtc)
{
    public static PaymentDto From(Payment p) => new(
        p.Id, p.MemberId, p.MembershipId, p.Amount, p.Method, p.Status,
        p.GatewayReference, p.FailureReason, p.PaidAtUtc, p.Notes, p.CreatedAtUtc);
}
