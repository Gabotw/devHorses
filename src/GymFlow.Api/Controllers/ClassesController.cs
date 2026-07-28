using GymFlow.Api.Security;
using GymFlow.Application.Features.Classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.Api.Controllers;

/// <summary>
/// Gestión de clases por el staff (Fase 7): programar sesiones con cupo, ver la agenda con
/// ocupación, cancelar y ver/tomar asistencia del roster. Cualquier staff autenticado.
/// </summary>
[ApiController]
[Route("api/classes")]
[Authorize(Policy = Policies.Staff)]
public sealed class ClassesController(IClassService classes) : ControllerBase
{
    /// <summary>Agenda de clases por rango de fechas locales; sin rango, próximos 30 días desde hoy.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        => Ok(await classes.ListSessionsAsync(from, to, ct));

    [HttpGet("{sessionId:guid}")]
    public async Task<IActionResult> Get(Guid sessionId, CancellationToken ct)
        => Ok(await classes.GetSessionAsync(sessionId, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClassSessionRequest request, CancellationToken ct)
    {
        var created = await classes.CreateSessionAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { sessionId = created.Id }, created);
    }

    [HttpPost("{sessionId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid sessionId, CancellationToken ct)
        => Ok(await classes.CancelSessionAsync(sessionId, ct));

    /// <summary>Roster de la sesión: reservas con cupo, en espera e historial.</summary>
    [HttpGet("{sessionId:guid}/roster")]
    public async Task<IActionResult> Roster(Guid sessionId, CancellationToken ct)
        => Ok(await classes.GetRosterAsync(sessionId, ct));

    /// <summary>Marca asistencia de un miembro con cupo confirmado.</summary>
    [HttpPost("{sessionId:guid}/attendance/{memberId:guid}")]
    public async Task<IActionResult> MarkAttendance(Guid sessionId, Guid memberId, CancellationToken ct)
        => Ok(await classes.MarkAttendanceAsync(sessionId, memberId, ct));
}
