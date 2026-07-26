using System.Security.Claims;
using GymFlow.Api.Security;
using GymFlow.Application.Features.Me;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.Api.Controllers;

/// <summary>
/// Auto-atención del miembro (app). El memberId sale del claim `sub` del token, nunca del
/// cliente, así que un miembro solo puede ver/actuar sobre sus propios datos.
/// </summary>
[ApiController]
[Route("api/me")]
[Authorize(Policy = Policies.Member)]
public sealed class MeController(IMeService me) : ControllerBase
{
    [HttpGet("membership")]
    public async Task<IActionResult> Membership(CancellationToken ct)
    {
        var membership = await me.GetMembershipAsync(MemberId, ct);
        return membership is null ? NoContent() : Ok(membership);
    }

    [HttpGet("checkins")]
    public async Task<IActionResult> CheckIns(CancellationToken ct)
        => Ok(await me.GetCheckInsAsync(MemberId, ct));

    [HttpGet("payments")]
    public async Task<IActionResult> Payments(CancellationToken ct)
        => Ok(await me.GetPaymentsAsync(MemberId, ct));

    [HttpPost("checkins")]
    public async Task<IActionResult> SelfCheckIn(CancellationToken ct)
        => Ok(await me.SelfCheckInAsync(MemberId, ct));

    /// <summary>MemberId del token. `sub` se mapea a NameIdentifier por el handler JWT.</summary>
    private Guid MemberId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Guid.TryParse(raw, out var id)
                ? id
                : throw new InvalidOperationException("Token de miembro sin identificador válido.");
        }
    }
}
