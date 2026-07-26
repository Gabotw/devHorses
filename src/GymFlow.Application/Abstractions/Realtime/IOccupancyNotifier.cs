namespace GymFlow.Application.Abstractions.Realtime;

/// <summary>
/// Puerto para difundir el aforo en tiempo real. El adaptador (SignalR, en la Api) empuja
/// el nuevo conteo a los clientes de recepción del tenant. La Application no conoce el
/// transporte — igual que las pasarelas de pago detrás de su puerto.
/// </summary>
public interface IOccupancyNotifier
{
    /// <summary>Notifica el aforo actual (asistencias válidas del día) del tenant indicado.</summary>
    Task OccupancyChangedAsync(Guid tenantId, int occupancy, CancellationToken ct = default);
}
