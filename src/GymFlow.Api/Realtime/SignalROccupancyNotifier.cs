using GymFlow.Application.Abstractions.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace GymFlow.Api.Realtime;

/// <summary>
/// Adaptador del puerto <see cref="IOccupancyNotifier"/> sobre SignalR: empuja el aforo al
/// grupo del tenant. Con backplane Redis (si está configurado) el evento llega a los clientes
/// de recepción aunque estén conectados a otra instancia de la Api.
/// </summary>
public sealed class SignalROccupancyNotifier(IHubContext<OccupancyHub> hub) : IOccupancyNotifier
{
    public Task OccupancyChangedAsync(Guid tenantId, int occupancy, CancellationToken ct = default) =>
        hub.Clients
            .Group(OccupancyGroups.ForTenant(tenantId))
            .SendAsync("occupancyChanged", occupancy, ct);
}
