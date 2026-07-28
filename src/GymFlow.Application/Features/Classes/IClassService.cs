namespace GymFlow.Application.Features.Classes;

/// <summary>
/// Clases & reservas (Fase 7). El staff programa y gestiona sesiones; los miembros reservan
/// (con lista de espera al llenarse). Todo acotado al tenant por el global query filter.
/// </summary>
public interface IClassService
{
    // --- Staff ---
    Task<ClassSessionDto> CreateSessionAsync(CreateClassSessionRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ClassSessionDto>> ListSessionsAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<ClassSessionDto> GetSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<ClassSessionDto> CancelSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<ClassReservationDto>> GetRosterAsync(Guid sessionId, CancellationToken ct = default);
    Task<ClassReservationDto> MarkAttendanceAsync(Guid sessionId, Guid memberId, CancellationToken ct = default);

    // --- Miembro (app) ---
    Task<IReadOnlyList<MemberClassSessionDto>> ListUpcomingForMemberAsync(Guid memberId, CancellationToken ct = default);
    Task<ReserveResultDto> ReserveAsync(Guid sessionId, Guid memberId, CancellationToken ct = default);
    Task<MemberClassSessionDto> CancelReservationAsync(Guid sessionId, Guid memberId, CancellationToken ct = default);
    Task<IReadOnlyList<MyReservationDto>> ListMyReservationsAsync(Guid memberId, CancellationToken ct = default);
}
