using GymFlow.Api.Security;
using GymFlow.Application.Features.Plans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.Api.Controllers;

[ApiController]
[Route("api/plans")]
[Authorize(Policy = Policies.Staff)]
public sealed class PlansController(IMembershipPlanService plans) : ControllerBase
{
    /// <summary>Lista planes. Por defecto solo activos; includeInactive=true trae todos.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await plans.ListAsync(includeInactive, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await plans.GetAsync(id, ct));

    // Crear/editar/activar planes es gestión: Owner/Admin.

    [HttpPost]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Create([FromBody] CreatePlanRequest request, CancellationToken ct)
    {
        var created = await plans.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlanRequest request, CancellationToken ct)
        => Ok(await plans.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await plans.DeactivateAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await plans.ActivateAsync(id, ct);
        return NoContent();
    }
}
