namespace GymFlow.Application.Features.MemberAuth;

public interface IMemberAuthService
{
    /// <summary>Devuelve el token del miembro o null si las credenciales no son válidas.</summary>
    Task<MemberLoginResult?> LoginAsync(MemberLoginRequest request, CancellationToken ct = default);
}
