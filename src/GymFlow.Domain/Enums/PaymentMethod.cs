namespace GymFlow.Domain.Enums;

/// <summary>
/// Medio con el que el miembro pagó en recepción. El cobro ocurre fuera del sistema;
/// aquí solo se deja constancia. Por ahora solo <see cref="Cash"/> (registro manual).
/// </summary>
public enum PaymentMethod
{
    Cash = 1,
}
