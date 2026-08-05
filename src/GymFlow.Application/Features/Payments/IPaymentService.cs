namespace GymFlow.Application.Features.Payments;

public interface IPaymentService
{
    Task<IReadOnlyList<PaymentDto>> ListByMemberAsync(Guid memberId, CancellationToken ct = default);

    /// <summary>Registra un pago en efectivo ya cobrado en recepción.</summary>
    Task<PaymentDto> RegisterCashAsync(RegisterCashPaymentRequest request, CancellationToken ct = default);
}
