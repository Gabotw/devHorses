namespace GymFlow.Application.Features.Platform;

/// <summary>Login del super-admin de plataforma (Fase 6). Opera fuera de todo tenant.</summary>
public interface IPlatformAuthService
{
    Task<PlatformLoginResult?> LoginAsync(PlatformLoginRequest request, CancellationToken ct = default);
}
