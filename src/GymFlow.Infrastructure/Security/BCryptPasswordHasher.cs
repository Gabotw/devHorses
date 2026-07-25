using GymFlow.Application.Abstractions.Security;

namespace GymFlow.Infrastructure.Security;

/// <summary>Adaptador de hashing con BCrypt (work factor 11).</summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash malformado (p.ej. placeholder inválido): trata como no verificado.
            return false;
        }
    }
}
