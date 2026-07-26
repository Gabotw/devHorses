using GymFlow.Api.Security;
using GymFlow.Application.Features.CheckIns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.Api.Controllers;

[ApiController]
[Route("api/checkins")]
[Authorize(Policy = Policies.Staff)]
public sealed class CheckInsController(ICheckInService checkIns) : ControllerBase
{
    /// <summary>Registra un ingreso (recepción). Devuelve el check-in y el aforo resultante.</summary>
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterCheckInRequest request, CancellationToken ct)
        => Ok(await checkIns.RegisterAsync(request, ct));

    /// <summary>Asistencia del día en la zona del tenant.</summary>
    [HttpGet("today")]
    public async Task<IActionResult> Today(CancellationToken ct)
        => Ok(await checkIns.ListTodayAsync(ct));

    /// <summary>Aforo actual (ingresos válidos del día).</summary>
    [HttpGet("occupancy")]
    public async Task<IActionResult> Occupancy(CancellationToken ct)
        => Ok(new { occupancy = await checkIns.GetOccupancyAsync(ct) });
}
