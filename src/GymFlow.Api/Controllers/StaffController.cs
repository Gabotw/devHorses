using GymFlow.Api.Security;
using GymFlow.Application.Features.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.Api.Controllers;

/// <summary>Gestión de usuarios del panel (staff). Solo Owner/Admin (policy Manager).</summary>
[ApiController]
[Route("api/staff")]
[Authorize(Policy = Policies.Manager)]
public sealed class StaffController(IStaffService staff) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await staff.ListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStaffRequest request, CancellationToken ct)
    {
        var created = await staff.CreateAsync(request, ct);
        return CreatedAtAction(nameof(List), null, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStaffRequest request, CancellationToken ct)
        => Ok(await staff.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetStaffPasswordRequest request, CancellationToken ct)
    {
        await staff.ResetPasswordAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await staff.ActivateAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await staff.DeactivateAsync(id, ct);
        return NoContent();
    }
}
