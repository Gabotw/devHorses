namespace GymFlow.Infrastructure.Payments;

/// <summary>
/// Configuración de Culqi. La llave secreta NUNCA va al repo: se inyecta vía
/// user-secrets / variables de entorno (igual que la connection string de Neon).
/// A futuro esto debería ser por-tenant; en la etapa de validación es global.
/// </summary>
public sealed class CulqiSettings
{
    public const string SectionName = "Culqi";

    public string BaseUrl { get; set; } = "https://api.culqi.com/v2";

    /// <summary>Llave secreta (sk_test_... / sk_live_...). Vacía = pasarela no configurada.</summary>
    public string SecretKey { get; set; } = string.Empty;
}
