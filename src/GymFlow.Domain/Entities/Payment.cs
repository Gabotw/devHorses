using GymFlow.Domain.Common;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

/// <summary>
/// Pago de un miembro (típicamente por una membresía). El monto es siempre
/// <see cref="decimal"/> (no-negociable). El efectivo se registra ya completado;
/// un cobro por pasarela nace <see cref="PaymentStatus.Pending"/> y se resuelve luego
/// con <see cref="Complete"/> o <see cref="Fail"/> según responda el gateway.
/// </summary>
public class Payment : Entity, ITenantScoped
{
    // EF Core
    private Payment() { }

    private Payment(Guid tenantId, Guid memberId, Guid? membershipId, decimal amount, PaymentMethod method)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId es obligatorio.", nameof(tenantId));
        if (memberId == Guid.Empty)
            throw new ArgumentException("MemberId es obligatorio.", nameof(memberId));
        if (amount <= 0m)
            throw new DomainException("El monto del pago debe ser mayor a cero.");

        TenantId = tenantId;
        MemberId = memberId;
        MembershipId = membershipId;
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Method = method;
    }

    public Guid TenantId { get; private set; }

    public Guid MemberId { get; private set; }

    /// <summary>Membresía que este pago cubre. Puede ser null (p.ej. venta suelta).</summary>
    public Guid? MembershipId { get; private set; }

    /// <summary>Monto cobrado. Decimal, redondeado a 2 decimales.</summary>
    public decimal Amount { get; private set; }

    public PaymentMethod Method { get; private set; }

    public PaymentStatus Status { get; private set; }

    /// <summary>Referencia del pago en la pasarela (charge id de Culqi/Izipay). Null si es efectivo.</summary>
    public string? GatewayReference { get; private set; }

    /// <summary>Motivo del rechazo cuando <see cref="Status"/> es <see cref="PaymentStatus.Failed"/>.</summary>
    public string? FailureReason { get; private set; }

    /// <summary>Instante (UTC) en que el pago quedó completado. Null mientras esté pendiente/fallido.</summary>
    public DateTime? PaidAtUtc { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>Registra un pago en efectivo ya cobrado (recepción). Nace completado.</summary>
    public static Payment RegisterCash(
        Guid tenantId, Guid memberId, Guid? membershipId, decimal amount, DateTime paidAtUtc, string? notes = null)
    {
        var payment = new Payment(tenantId, memberId, membershipId, amount, PaymentMethod.Cash)
        {
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
        };
        payment.Complete(gatewayReference: null, paidAtUtc);
        return payment;
    }

    /// <summary>Inicia un cobro por pasarela. Queda pendiente hasta la respuesta del gateway.</summary>
    public static Payment StartGatewayCharge(
        Guid tenantId, Guid memberId, Guid? membershipId, decimal amount, PaymentMethod method, string? notes = null)
    {
        if (method == PaymentMethod.Cash)
            throw new DomainException("El efectivo no pasa por pasarela; usa RegisterCash.");

        return new Payment(tenantId, memberId, membershipId, amount, method)
        {
            Status = PaymentStatus.Pending,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
        };
    }

    /// <summary>Marca el pago como completado (efectivo cobrado o pasarela aprobada).</summary>
    public void Complete(string? gatewayReference, DateTime paidAtUtc)
    {
        if (Status == PaymentStatus.Completed)
            return;
        if (Status == PaymentStatus.Refunded)
            throw new DomainException("No se puede completar un pago reembolsado.");

        Status = PaymentStatus.Completed;
        GatewayReference = string.IsNullOrWhiteSpace(gatewayReference) ? null : gatewayReference.Trim();
        FailureReason = null;
        PaidAtUtc = DateTime.SpecifyKind(paidAtUtc, DateTimeKind.Utc);
        Touch();
    }

    /// <summary>Marca el cobro como rechazado por la pasarela.</summary>
    public void Fail(string reason)
    {
        Status = PaymentStatus.Failed;
        FailureReason = string.IsNullOrWhiteSpace(reason) ? "Cobro rechazado." : reason.Trim();
        PaidAtUtc = null;
        Touch();
    }
}
