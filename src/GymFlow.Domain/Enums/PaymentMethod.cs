namespace GymFlow.Domain.Enums;

/// <summary>
/// Medio con el que el miembro pagó. <see cref="Cash"/> es el registro manual (Fase 2);
/// <see cref="Culqi"/>/<see cref="Izipay"/> son pasarelas (detrás de <c>IPaymentGateway</c>).
/// </summary>
public enum PaymentMethod
{
    Cash = 1,
    Culqi = 2,
    Izipay = 3,
}
