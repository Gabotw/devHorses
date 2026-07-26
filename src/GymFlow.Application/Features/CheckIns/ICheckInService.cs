namespace GymFlow.Application.Features.CheckIns;

public interface ICheckInService
{
    /// <summary>Registra un ingreso validando la membresía vigente. Difunde el nuevo aforo.</summary>
    Task<CheckInResultDto> RegisterAsync(RegisterCheckInRequest request, CancellationToken ct = default);

    /// <summary>Asistencia del día (zona del tenant), más reciente primero.</summary>
    Task<IReadOnlyList<CheckInDto>> ListTodayAsync(CancellationToken ct = default);

    /// <summary>Historial de asistencia de un miembro (más reciente primero).</summary>
    Task<IReadOnlyList<CheckInDto>> ListByMemberAsync(Guid memberId, int take = 50, CancellationToken ct = default);

    /// <summary>Aforo actual: número de ingresos válidos del día.</summary>
    Task<int> GetOccupancyAsync(CancellationToken ct = default);
}
