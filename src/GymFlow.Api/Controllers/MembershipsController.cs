using GymFlow.Api.Security;
using GymFlow.Application.Features.Memberships;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.Api.Controllers;

[ApiController]
[Route("api/memberships")]
[Authorize(Policy = Policies.Staff)]
public sealed class MembershipsController(IMembershipService memberships) : ControllerBase
{
    /// <summary>Asigna un plan a un miembro (crea la membresía).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMembershipRequest request, CancellationToken ct)
    {
        var created = await memberships.CreateAsync(request, ct);
        return CreatedAtAction(nameof(ListByMember), new { memberId = created.MemberId }, created);
    }

    [HttpGet("by-member/{memberId:guid}")]
    public async Task<IActionResult> ListByMember(Guid memberId, CancellationToken ct)
        => Ok(await memberships.ListByMemberAsync(memberId, ct));

    /// <summary>Membresías por vencer (o ya vencidas) dentro de N días, para avisar a los clientes.</summary>
    [HttpGet("expiring")]
    public async Task<IActionResult> Expiring([FromQuery] int withinDays = 7, CancellationToken ct = default)
        => Ok(await memberships.ListExpiringAsync(withinDays, ct));

    [HttpPost("{id:guid}/freeze")]
    public async Task<IActionResult> Freeze(Guid id, [FromBody] FreezeMembershipRequest request, CancellationToken ct)
        => Ok(await memberships.FreezeAsync(id, request, ct));

    [HttpPost("{id:guid}/unfreeze")]
    public async Task<IActionResult> Unfreeze(Guid id, [FromBody] UnfreezeMembershipRequest request, CancellationToken ct)
        => Ok(await memberships.UnfreezeAsync(id, request, ct));
}
