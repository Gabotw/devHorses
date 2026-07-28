using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Abstractions.Security;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Features.Platform;

/// <summary>
/// Login del super-admin. Los <c>PlatformAdmins</c> son de nivel plataforma (sin global query
/// filter), así que la búsqueda es directa por email a través de toda la tabla. Emite un token
/// con actor=platform (sin tenant_id).
/// </summary>
public sealed class PlatformAuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : IPlatformAuthService
{
    // Hash BCrypt de una contraseña aleatoria; iguala el costo de CPU cuando el email no existe
    // (mitigación básica de user enumeration por timing).
    private const string PlaceholderHash = "$2a$11$C6UzMDM.H6dfI/f/IKcEeO3JZ5j0y0jLg3jZ0m5cQ1JcT8j3q3mYy";

    public async Task<PlatformLoginResult?> LoginAsync(PlatformLoginRequest request, CancellationToken ct = default)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (email.Length == 0 || string.IsNullOrEmpty(request.Password))
            return null;

        var admin = await db.PlatformAdmins.FirstOrDefaultAsync(a => a.Email == email, ct);

        var hashToCheck = admin?.PasswordHash ?? PlaceholderHash;
        var passwordOk = passwordHasher.Verify(request.Password, hashToCheck);

        if (admin is null || !admin.IsActive || !passwordOk)
            return null;

        admin.RegisterLogin();
        await db.SaveChangesAsync(ct);

        var token = jwtTokenService.IssueForPlatformAdmin(admin);
        return new PlatformLoginResult(token.Token, token.ExpiresAtUtc, admin.Id, admin.FullName);
    }
}
