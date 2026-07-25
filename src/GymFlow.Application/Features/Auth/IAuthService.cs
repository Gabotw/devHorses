namespace GymFlow.Application.Features.Auth;

public interface IAuthService
{
    /// <summary>
    /// Autentica un usuario de staff dentro del tenant ya resuelto por el middleware.
    /// Devuelve null si las credenciales son inválidas o el usuario está inactivo.
    /// </summary>
    Task<LoginResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
