using GymFlow.Api.Security;
using GymFlow.Application.Features.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.Api.Controllers;

/// <summary>
/// Gestión del billing de cada gimnasio por el super-admin (Fase 6): ver los tenants con su
/// suscripción y asignar/renovar/cancelar su plan. Solo super-admin (actor=platform).
/// </summary>
[ApiController]
[Route("api/platform/tenants")]
[Authorize(Policy = Policies.Platform)]
public sealed class PlatformTenantsController(IPlatformBillingService billing) : ControllerBase
{
    /// <summary>Todos los gimnasios con su suscripción vigente y conteo de miembros.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await billing.ListTenantsAsync(ct));

    [HttpGet("{tenantId:guid}")]
    public async Task<IActionResult> Get(Guid tenantId, CancellationToken ct)
        => Ok(await billing.GetTenantAsync(tenantId, ct));

    /// <summary>Asigna o cambia el plan del gimnasio (crea o reemplaza su suscripción vigente).</summary>
    [HttpPost("{tenantId:guid}/subscription")]
    public async Task<IActionResult> Assign(Guid tenantId, [FromBody] AssignSubscriptionRequest request, CancellationToken ct)
        => Ok(await billing.AssignAsync(tenantId, request, ct));

    /// <summary>Renueva el período de la suscripción vigente (mismo plan) desde hoy.</summary>
    [HttpPost("{tenantId:guid}/subscription/renew")]
    public async Task<IActionResult> Renew(Guid tenantId, CancellationToken ct)
        => Ok(await billing.RenewAsync(tenantId, ct));

    [HttpPost("{tenantId:guid}/subscription/cancel")]
    public async Task<IActionResult> Cancel(Guid tenantId, CancellationToken ct)
        => Ok(await billing.CancelAsync(tenantId, ct));
}
