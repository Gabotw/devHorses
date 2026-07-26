namespace GymFlow.Application.Abstractions.Payments;

/// <summary>
/// Puerto de pasarela de pago. Los adaptadores concretos (Culqi/Izipay) viven en
/// Infrastructure; la Application solo conoce este contrato — igual que el aislamiento
/// de SUNAT en el invoicing SaaS. Nunca metas SDKs de pasarela fuera de Infrastructure.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Nombre del proveedor (p.ej. "culqi"). Sirve para logs y ruteo.</summary>
    string Name { get; }

    /// <summary>Intenta cobrar. No lanza por rechazos de negocio: los reporta en el resultado.</summary>
    Task<PaymentGatewayResult> ChargeAsync(PaymentChargeRequest request, CancellationToken ct = default);
}

/// <summary>Datos para intentar un cobro. El monto se envía a la pasarela en su unidad mínima.</summary>
public sealed record PaymentChargeRequest(
    decimal Amount,
    string Currency,
    string SourceToken,
    string Email,
    string Description);

/// <summary>Resultado del intento de cobro. <see cref="Reference"/> es el id del cargo si aprobó.</summary>
public sealed record PaymentGatewayResult(bool Success, string? Reference, string? Error)
{
    public static PaymentGatewayResult Ok(string reference) => new(true, reference, null);
    public static PaymentGatewayResult Fail(string error) => new(false, null, error);
}
