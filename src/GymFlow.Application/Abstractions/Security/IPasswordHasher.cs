namespace GymFlow.Application.Abstractions.Security;

/// <summary>
/// Puerto de hashing de contraseñas. El adaptador (BCrypt) vive en Infrastructure.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
