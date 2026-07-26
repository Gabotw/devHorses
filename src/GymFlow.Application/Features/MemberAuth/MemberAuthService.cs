using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Abstractions.Security;
using GymFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Features.MemberAuth;

/// <summary>
/// Login del miembro para la app. Igual que el login de staff, se apoya en el global query
/// filter: el documento se busca dentro del tenant de la request (resuelto por subdominio),
/// nunca se confía en un TenantId del cliente. Solo pueden entrar miembros activos y con
/// contraseña asignada (acceso a la app).
/// </summary>
public sealed class MemberAuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : IMemberAuthService
{
    // Hash BCrypt de una contraseña aleatoria; iguala el costo de CPU cuando el documento
    // no existe (mitigación de enumeración por timing).
    private const string PlaceholderHash = "$2a$11$C6UzMDM.H6dfI/f/IKcEeO3JZ5j0y0jLg3jZ0m5cQ1JcT8j3q3mYy";

    public async Task<MemberLoginResult?> LoginAsync(MemberLoginRequest request, CancellationToken ct = default)
    {
        var documentId = (request.DocumentId ?? string.Empty).Trim();
        if (documentId.Length == 0 || string.IsNullOrEmpty(request.Password))
            return null;

        var member = await db.Members.FirstOrDefaultAsync(m => m.DocumentId == documentId, ct);

        var hashToCheck = member?.PasswordHash ?? PlaceholderHash;
        var passwordOk = passwordHasher.Verify(request.Password, hashToCheck);

        if (member is null || !member.HasAppAccess || member.Status != MemberStatus.Active || !passwordOk)
            return null;

        member.RegisterLogin();
        await db.SaveChangesAsync(ct);

        var token = jwtTokenService.IssueForMember(member);
        return new MemberLoginResult(token.Token, token.ExpiresAtUtc, member.Id, member.FullName);
    }
}
