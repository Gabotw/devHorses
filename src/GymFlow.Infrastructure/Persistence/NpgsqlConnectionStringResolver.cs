using Npgsql;

namespace GymFlow.Infrastructure.Persistence;

/// <summary>
/// Normaliza la cadena de conexión a un formato válido para Npgsql. Acepta tanto el formato
/// key/value (`Host=...;Database=...`) como la URI que dan por defecto Neon/Postgres
/// (`postgresql://user:pass@host/db?sslmode=require`). La URI es más robusta porque la
/// contraseña viaja URL-encoded, evitando que caracteres especiales (`;`, `=`, espacios)
/// rompan el parseo del formato key/value.
/// </summary>
public static class NpgsqlConnectionStringResolver
{
    public static string Resolve(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;

        var value = raw.Trim();
        var isUri = value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                    || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

        return isUri ? FromUri(value) : value;
    }

    private static string FromUri(string uriString)
    {
        var uri = new Uri(uriString);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/')),
            // Neon exige TLS y presenta un certificado de CA pública, así que VerifyFull
            // funciona y es lo más seguro (los parámetros sslmode/channel_binding de la URI
            // quedan cubiertos por esto).
            SslMode = SslMode.VerifyFull,
        };

        return builder.ConnectionString;
    }
}
