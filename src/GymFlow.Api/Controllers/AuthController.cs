using GymFlow.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Login de staff dentro del tenant resuelto (subdominio o X-Tenant-Id).
    /// El middleware ya fijó el tenant; aquí solo se validan credenciales.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return result is null
            ? Unauthorized(new { error = "Credenciales inválidas." })
            : Ok(result);
    }

    /// <summary>Devuelve la identidad del usuario autenticado (smoke test de JWT + tenant).</summary>
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            name = User.Identity?.Name,
            tenantId = User.FindFirst(Security.GymFlowClaims.TenantId)?.Value,
            role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value,
        });
    }
}
