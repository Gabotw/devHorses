using GymFlow.Api.Security;
using GymFlow.Application.Features.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.Api.Controllers;

/// <summary>
/// Reportes del panel (Fase 5). Es información de gestión, así que exige rol Manager
/// (owner/admin); la recepción no ve ingresos ni churn.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize(Policy = Policies.Manager)]
public sealed class ReportsController(IReportService reports) : ControllerBase
{
    /// <summary>
    /// Dashboard del tenant. <paramref name="from"/>/<paramref name="to"/> son fechas locales
    /// (zona del tenant); si se omiten, cubre los últimos 30 días terminando hoy.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(await reports.GetDashboardAsync(from, to, ct));
}
