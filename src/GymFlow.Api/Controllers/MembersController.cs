using GymFlow.Api.Security;
using GymFlow.Application.Features.Members;
using GymFlow.Application.Features.Memberships;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.Api.Controllers;

[ApiController]
[Route("api/members")]
[Authorize(Policy = Policies.Staff)]
public sealed class MembersController(
    IMemberService members,
    IMembershipService memberships) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await members.ListAsync(search, page, pageSize, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await members.GetAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMemberRequest request, CancellationToken ct)
    {
        var created = await members.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMemberRequest request, CancellationToken ct)
        => Ok(await members.UpdateAsync(id, request, ct));

    /// <summary>Baja lógica del miembro. Restringido a Owner/Admin.</summary>
    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await members.DeactivateAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await members.ActivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>Genera un nuevo código de acceso de 4 dígitos para el miembro.</summary>
    [HttpPost("{id:guid}/regenerate-code")]
    public async Task<IActionResult> RegenerateCode(Guid id, CancellationToken ct)
        => Ok(await members.RegenerateAccessCodeAsync(id, ct));

    // --- Membresías del miembro ---

    [HttpGet("{id:guid}/memberships")]
    public async Task<IActionResult> ListMemberships(Guid id, CancellationToken ct)
        => Ok(await memberships.ListByMemberAsync(id, ct));

    [HttpGet("{id:guid}/memberships/current")]
    public async Task<IActionResult> CurrentMembership(Guid id, CancellationToken ct)
    {
        var current = await memberships.GetCurrentAsync(id, ct);
        return current is null ? NoContent() : Ok(current);
    }
}
