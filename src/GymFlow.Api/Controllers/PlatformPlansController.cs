using GymFlow.Api.Security;
using GymFlow.Application.Features.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.Api.Controllers;

/// <summary>
/// Catálogo de planes de la plataforma (billing SaaS, Fase 6). Solo super-admin (actor=platform).
/// </summary>
[ApiController]
[Route("api/platform/plans")]
[Authorize(Policy = Policies.Platform)]
public sealed class PlatformPlansController(IPlatformPlanService plans) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive, CancellationToken ct)
        => Ok(await plans.ListAsync(includeInactive, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertPlatformPlanRequest request, CancellationToken ct)
    {
        var created = await plans.CreateAsync(request, ct);
        return CreatedAtAction(nameof(List), new { }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertPlatformPlanRequest request, CancellationToken ct)
        => Ok(await plans.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
        => Ok(await plans.SetActiveAsync(id, isActive: true, ct));

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        => Ok(await plans.SetActiveAsync(id, isActive: false, ct));
}
