using GymFlow.Domain.Common;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

/// <summary>
/// Período de membresía de un miembro bajo un plan. Guarda un snapshot del precio y
/// la duración del plan al momento de la compra (los planes pueden cambiar después).
///
/// Fechas de período como <see cref="DateOnly"/> (son días de calendario, no instantes);
/// los timestamps de auditoría siguen en UTC en <see cref="Entity"/>.
///
/// Transiciones de Fase 1:
///   Active → Frozen → Active   (congelar / descongelar)
///   Active → Expired           (vencimiento)
/// La transición a Overdue (morosa) llega en Fase 2.
/// </summary>
public class Membership : Entity, ITenantScoped
{
    // EF Core
    private Membership() { }

    public Membership(Guid tenantId, Member member, MembershipPlan plan, DateOnly startDate)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId es obligatorio.", nameof(tenantId));
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(plan);

        if (member.TenantId != tenantId || plan.TenantId != tenantId)
            throw new DomainException("Miembro y plan deben pertenecer al mismo tenant.");

        TenantId = tenantId;
        MemberId = member.Id;
        PlanId = plan.Id;
        PriceAtPurchase = plan.Price;
        DurationDaysAtPurchase = plan.DurationDays;
        StartDate = startDate;
        EndDate = startDate.AddDays(plan.DurationDays);
        Status = MembershipStatus.Active;
    }

    public Guid TenantId { get; private set; }

    public Guid MemberId { get; private set; }

    public Guid PlanId { get; private set; }

    /// <summary>Precio cobrado al comprar (snapshot). Decimal.</summary>
    public decimal PriceAtPurchase { get; private set; }

    public int DurationDaysAtPurchase { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public MembershipStatus Status { get; private set; }

    public DateOnly? FrozenFrom { get; private set; }

    public DateOnly? FrozenUntil { get; private set; }

    /// <summary>Congela la membresía. El tiempo congelado se suma al vencimiento.</summary>
    public void Freeze(DateOnly from, DateOnly until)
    {
        if (Status != MembershipStatus.Active)
            throw new DomainException("Solo se puede congelar una membresía activa.");
        if (until <= from)
            throw new DomainException("La fecha de fin de congelamiento debe ser posterior al inicio.");

        var frozenDays = until.DayNumber - from.DayNumber;
        FrozenFrom = from;
        FrozenUntil = until;
        EndDate = EndDate.AddDays(frozenDays);
        Status = MembershipStatus.Frozen;
        Touch();
    }

    /// <summary>Reactiva una membresía congelada. Si se reanuda antes de lo previsto,
    /// devuelve al vencimiento los días no usados del congelamiento.</summary>
    public void Unfreeze(DateOnly resumeDate)
    {
        if (Status != MembershipStatus.Frozen)
            throw new DomainException("La membresía no está congelada.");

        if (FrozenUntil is { } until && resumeDate < until)
        {
            var unusedDays = until.DayNumber - resumeDate.DayNumber;
            EndDate = EndDate.AddDays(-unusedDays);
        }

        FrozenFrom = null;
        FrozenUntil = null;
        Status = MembershipStatus.Active;
        Touch();
    }

    /// <summary>Marca la membresía como vencida.</summary>
    public void MarkExpired()
    {
        if (Status == MembershipStatus.Expired)
            return;
        Status = MembershipStatus.Expired;
        Touch();
    }

    /// <summary>
    /// Recalcula el estado respecto a una fecha (típicamente hoy en la zona del tenant):
    /// una membresía activa cuyo fin ya pasó se marca vencida. No toca las congeladas.
    /// </summary>
    public void RecalculateStatus(DateOnly today)
    {
        if (Status == MembershipStatus.Active && today > EndDate)
            MarkExpired();
    }

    public bool IsActiveOn(DateOnly date) =>
        Status == MembershipStatus.Active && date >= StartDate && date <= EndDate;
}
