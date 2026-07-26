using GymFlow.Application.Features.MemberAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.Api.Controllers;

[ApiController]
[Route("api/member-auth")]
public sealed class MemberAuthController(IMemberAuthService memberAuth) : ControllerBase
{
    /// <summary>
    /// Login del miembro (app) dentro del tenant resuelto por subdominio (header
    /// X-Tenant-Subdomain en localhost). Devuelve un token con actor=member.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(MemberLoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] MemberLoginRequest request, CancellationToken ct)
    {
        var result = await memberAuth.LoginAsync(request, ct);
        return result is null
            ? Unauthorized(new { error = "Credenciales inválidas." })
            : Ok(result);
    }
}
