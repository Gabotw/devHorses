using System.Security.Claims;
using GymFlow.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GymFlow.Api.Realtime;

/// <summary>
/// Hub de aforo en tiempo real para recepción. Al conectar, la conexión se une al grupo de
/// su tenant (leído del claim del JWT, no del cliente) y recibe eventos "occupancyChanged".
/// El JWT viaja por query string en el handshake (ver OnMessageReceived en Program).
/// </summary>
[Authorize(Policy = Policies.Staff)]
public sealed class OccupancyHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirstValue(GymFlowClaims.TenantId);
        if (!string.IsNullOrWhiteSpace(tenantId))
            await Groups.AddToGroupAsync(Context.ConnectionId, OccupancyGroups.ForTenant(tenantId));

        await base.OnConnectedAsync();
    }
}
