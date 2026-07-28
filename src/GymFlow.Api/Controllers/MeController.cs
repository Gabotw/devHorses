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

    // --- Clases (Fase 7) ---

    /// <summary>Próximas clases con cupo disponible y el estado de reserva del propio miembro.</summary>
    [HttpGet("classes")]
    public async Task<IActionResult> UpcomingClasses(CancellationToken ct)
        => Ok(await me.GetUpcomingClassesAsync(MemberId, ct));

    /// <summary>Reserva una clase. Si está llena, entra a lista de espera.</summary>
    [HttpPost("classes/{sessionId:guid}/reserve")]
    public async Task<IActionResult> Reserve(Guid sessionId, CancellationToken ct)
        => Ok(await me.ReserveClassAsync(MemberId, sessionId, ct));

    /// <summary>Cancela la reserva del miembro (promueve al primero en espera si tenía cupo).</summary>
    [HttpPost("classes/{sessionId:guid}/cancel")]
    public async Task<IActionResult> CancelReservation(Guid sessionId, CancellationToken ct)
        => Ok(await me.CancelClassReservationAsync(MemberId, sessionId, ct));

    /// <summary>Reservas del miembro (agenda/historial).</summary>
    [HttpGet("reservations")]
    public async Task<IActionResult> MyReservations(CancellationToken ct)
        => Ok(await me.GetMyReservationsAsync(MemberId, ct));

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
