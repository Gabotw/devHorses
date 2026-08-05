using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Abstractions.Tenancy;
using GymFlow.Application.Abstractions.Time;
using GymFlow.Application.Common;
using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Features.Payments;

/// <summary>
/// Registro de pagos cobrados en recepción (el pago se hace fuera del sistema; aquí solo
/// se deja constancia). El pago solo registra dinero: renovar/reactivar una membresía es
/// una acción aparte del flujo de membresías (asignar un nuevo período).
/// </summary>
public sealed class PaymentService(
    IAppDbContext db,
    ITenantProvider tenant,
    IClock clock) : IPaymentService
{
    public async Task<IReadOnlyList<PaymentDto>> ListByMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        return await db.Payments.AsNoTracking()
            .Where(p => p.MemberId == memberId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => PaymentDto.From(p))
            .ToListAsync(ct);
    }

    public async Task<PaymentDto> RegisterCashAsync(RegisterCashPaymentRequest request, CancellationToken ct = default)
    {
        var tenantId = tenant.GetRequiredTenantId();
        var member = await GetMemberAsync(request.MemberId, ct);
        var membership = await GetMembershipForAsync(request.MembershipId, member.Id, ct);

        var payment = Payment.RegisterCash(
            tenantId, member.Id, membership?.Id, request.Amount, clock.UtcNow, request.Notes);
        db.Payments.Add(payment);

        await db.SaveChangesAsync(ct);
        return PaymentDto.From(payment);
    }

    private async Task<Member> GetMemberAsync(Guid memberId, CancellationToken ct)
        => await db.Members.FirstOrDefaultAsync(m => m.Id == memberId, ct)
            ?? throw new NotFoundException("Miembro no encontrado.");

    private async Task<Membership?> GetMembershipForAsync(Guid? membershipId, Guid memberId, CancellationToken ct)
    {
        if (membershipId is null)
            return null;

        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId, ct)
            ?? throw new NotFoundException("Membresía no encontrada.");

        if (membership.MemberId != memberId)
            throw new ConflictException("La membresía no pertenece al miembro indicado.");

        return membership;
    }
}
