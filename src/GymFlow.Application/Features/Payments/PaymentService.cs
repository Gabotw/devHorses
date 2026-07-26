using GymFlow.Application.Abstractions.Payments;
using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Abstractions.Tenancy;
using GymFlow.Application.Abstractions.Time;
using GymFlow.Application.Common;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Features.Payments;

/// <summary>
/// Registro y cobro de pagos. El efectivo se registra ya completado; el cobro por
/// pasarela pasa por <see cref="IPaymentGateway"/> (adaptador Culqi en Infrastructure).
/// El pago solo registra dinero: renovar/reactivar una membresía es una acción aparte
/// del flujo de membresías (asignar un nuevo período).
/// </summary>
public sealed class PaymentService(
    IAppDbContext db,
    ITenantProvider tenant,
    IClock clock,
    IPaymentGateway gateway) : IPaymentService
{
    private const string Currency = "PEN";

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

    public async Task<PaymentDto> ChargeAsync(ChargePaymentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.SourceToken))
            throw new ConflictException("Falta el token de la tarjeta para cobrar.");

        var tenantId = tenant.GetRequiredTenantId();
        var member = await GetMemberAsync(request.MemberId, ct);
        var membership = await GetMembershipForAsync(request.MembershipId, member.Id, ct);

        var payment = Payment.StartGatewayCharge(
            tenantId, member.Id, membership?.Id, request.Amount, PaymentMethod.Culqi, request.Notes);
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        var result = await gateway.ChargeAsync(
            new PaymentChargeRequest(
                payment.Amount, Currency, request.SourceToken,
                member.Email ?? $"{member.DocumentId}@sin-correo.local",
                $"Membresía GymFlow — {member.FullName}"),
            ct);

        if (result.Success)
            payment.Complete(result.Reference, clock.UtcNow);
        else
            payment.Fail(result.Error ?? "Cobro rechazado por la pasarela.");

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
