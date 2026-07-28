using GymFlow.Application.Features.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.Api.Controllers;

/// <summary>
/// Login del super-admin de plataforma (billing SaaS, Fase 6). No requiere tenant: el token
/// emitido lleva actor=platform (sin tenant_id) y da acceso solo a /api/platform/*.
/// </summary>
[ApiController]
[Route("api/platform/auth")]
public sealed class PlatformAuthController(IPlatformAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(PlatformLoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] PlatformLoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return result is null
            ? Unauthorized(new { error = "Credenciales inválidas." })
            : Ok(result);
    }
}
