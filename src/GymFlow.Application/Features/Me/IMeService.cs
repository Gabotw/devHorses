using GymFlow.Application.Features.Classes;
using GymFlow.Application.Features.CheckIns;
using GymFlow.Application.Features.Payments;

namespace GymFlow.Application.Features.Me;

/// <summary>
/// Servicios de auto-atención del miembro (app). El memberId lo aporta el controller
/// desde el claim `sub` del token — nunca del cuerpo de la petición.
/// </summary>
public interface IMeService
{
    Task<MyMembershipDto?> GetMembershipAsync(Guid memberId, CancellationToken ct = default);

    Task<IReadOnlyList<CheckInDto>> GetCheckInsAsync(Guid memberId, CancellationToken ct = default);

    Task<IReadOnlyList<PaymentDto>> GetPaymentsAsync(Guid memberId, CancellationToken ct = default);

    /// <summary>Auto check-in desde la app (método App). Devuelve el check-in y el aforo.</summary>
    Task<CheckInResultDto> SelfCheckInAsync(Guid memberId, CancellationToken ct = default);

    // Clases (Fase 7)
    Task<IReadOnlyList<MemberClassSessionDto>> GetUpcomingClassesAsync(Guid memberId, CancellationToken ct = default);
    Task<ReserveResultDto> ReserveClassAsync(Guid memberId, Guid sessionId, CancellationToken ct = default);
    Task<MemberClassSessionDto> CancelClassReservationAsync(Guid memberId, Guid sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<MyReservationDto>> GetMyReservationsAsync(Guid memberId, CancellationToken ct = default);
}
