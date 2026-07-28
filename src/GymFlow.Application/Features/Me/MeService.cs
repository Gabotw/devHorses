using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Features.Classes;
using GymFlow.Application.Features.CheckIns;
using GymFlow.Application.Features.Payments;
using GymFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Features.Me;

/// <summary>
/// Auto-atención del miembro. Reutiliza los servicios existentes (check-ins, pagos) y
/// proyecta la membresía con el nombre del plan. Todo queda acotado al tenant por el
/// global query filter; además cada consulta filtra por el memberId autenticado.
/// </summary>
public sealed class MeService(
    IAppDbContext db,
    ICheckInService checkIns,
    IPaymentService payments,
    IClassService classes) : IMeService
{
    public async Task<MyMembershipDto?> GetMembershipAsync(Guid memberId, CancellationToken ct = default)
    {
        // La más reciente del miembro, cualquiera sea su estado (activa/morosa/vencida),
        // para que la app pueda mostrar el estado real.
        return await db.Memberships.AsNoTracking()
            .Where(m => m.MemberId == memberId)
            .OrderByDescending(m => m.StartDate)
            .Join(db.MembershipPlans, m => m.PlanId, p => p.Id,
                (m, p) => new MyMembershipDto(
                    m.Id, p.Name, m.PriceAtPurchase, m.StartDate, m.EndDate, m.Status))
            .FirstOrDefaultAsync(ct);
    }

    public Task<IReadOnlyList<CheckInDto>> GetCheckInsAsync(Guid memberId, CancellationToken ct = default)
        => checkIns.ListByMemberAsync(memberId, ct: ct);

    public Task<IReadOnlyList<PaymentDto>> GetPaymentsAsync(Guid memberId, CancellationToken ct = default)
        => payments.ListByMemberAsync(memberId, ct);

    public Task<CheckInResultDto> SelfCheckInAsync(Guid memberId, CancellationToken ct = default)
        => checkIns.RegisterAsync(new RegisterCheckInRequest(memberId, CheckInMethod.App), ct);

    public Task<IReadOnlyList<MemberClassSessionDto>> GetUpcomingClassesAsync(Guid memberId, CancellationToken ct = default)
        => classes.ListUpcomingForMemberAsync(memberId, ct);

    public Task<ReserveResultDto> ReserveClassAsync(Guid memberId, Guid sessionId, CancellationToken ct = default)
        => classes.ReserveAsync(sessionId, memberId, ct);

    public Task<MemberClassSessionDto> CancelClassReservationAsync(Guid memberId, Guid sessionId, CancellationToken ct = default)
        => classes.CancelReservationAsync(sessionId, memberId, ct);

    public Task<IReadOnlyList<MyReservationDto>> GetMyReservationsAsync(Guid memberId, CancellationToken ct = default)
        => classes.ListMyReservationsAsync(memberId, ct);
}
