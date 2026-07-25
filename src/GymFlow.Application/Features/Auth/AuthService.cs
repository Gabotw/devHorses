using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Abstractions.Security;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Features.Auth;

/// <summary>
/// Login de staff. Se apoya en el global query filter: la consulta a StaffUsers
/// ya está acotada al tenant de la request, así que un email de otro tenant
/// simplemente no existe en el resultado. Nunca se pasa el TenantId del cliente.
/// </summary>
public sealed class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (email.Length == 0 || string.IsNullOrEmpty(request.Password))
            return null;

        var user = await db.StaffUsers
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        // Verifica el hash aun cuando el usuario no exista, para no filtrar por timing
        // si el email es válido o no (mitigación básica de user enumeration).
        var hashToCheck = user?.PasswordHash ?? PlaceholderHash;
        var passwordOk = passwordHasher.Verify(request.Password, hashToCheck);

        if (user is null || !user.IsActive || !passwordOk)
            return null;

        user.RegisterLogin();
        await db.SaveChangesAsync(cancellationToken);

        var token = jwtTokenService.Issue(user);
        return new LoginResult(
            token.Token,
            token.ExpiresAtUtc,
            user.Id,
            user.FullName,
            user.Role.ToString());
    }

    // Hash BCrypt válido de una contraseña aleatoria; solo para gastar tiempo de CPU
    // comparable cuando el email no existe.
    private const string PlaceholderHash = "$2a$11$C6UzMDM.H6dfI/f/IKcEeO3JZ5j0y0jLg3jZ0m5cQ1JcT8j3q3mYy";
}
