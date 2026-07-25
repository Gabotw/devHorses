namespace GymFlow.Api.Security;

/// <summary>Configuración de firma/emisión de JWT. Se enlaza desde la sección "Jwt".</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "gymflow";
    public string Audience { get; set; } = "gymflow-clients";

    /// <summary>Clave simétrica HMAC. En prod viene de variable de entorno/secret, nunca del repo.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;
}
