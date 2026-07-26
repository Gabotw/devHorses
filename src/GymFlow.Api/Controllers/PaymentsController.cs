using GymFlow.Api.Security;
using GymFlow.Application.Features.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Policy = Policies.Staff)]
public sealed class PaymentsController(IPaymentService payments) : ControllerBase
{
    [HttpGet("by-member/{memberId:guid}")]
    public async Task<IActionResult> ListByMember(Guid memberId, CancellationToken ct)
        => Ok(await payments.ListByMemberAsync(memberId, ct));

    /// <summary>Registra un pago en efectivo cobrado en recepción.</summary>
    [HttpPost("cash")]
    public async Task<IActionResult> RegisterCash([FromBody] RegisterCashPaymentRequest request, CancellationToken ct)
    {
        var created = await payments.RegisterCashAsync(request, ct);
        return CreatedAtAction(nameof(ListByMember), new { memberId = created.MemberId }, created);
    }

    /// <summary>Cobra por pasarela (Culqi) usando un token de tarjeta generado en el cliente.</summary>
    [HttpPost("charge")]
    public async Task<IActionResult> Charge([FromBody] ChargePaymentRequest request, CancellationToken ct)
    {
        var result = await payments.ChargeAsync(request, ct);
        return Ok(result);
    }
}
