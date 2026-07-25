using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.Features.Memberships;

/// <summary>Asigna un plan a un miembro. StartDate opcional: por defecto hoy (zona del tenant).</summary>
public sealed record CreateMembershipRequest(Guid MemberId, Guid PlanId, DateOnly? StartDate);

public sealed record FreezeMembershipRequest(DateOnly From, DateOnly Until);

public sealed record UnfreezeMembershipRequest(DateOnly ResumeDate);

public sealed record MembershipDto(
    Guid Id,
    Guid MemberId,
    Guid PlanId,
    decimal PriceAtPurchase,
    DateOnly StartDate,
    DateOnly EndDate,
    MembershipStatus Status,
    DateOnly? FrozenFrom,
    DateOnly? FrozenUntil)
{
    public static MembershipDto From(Membership m) => new(
        m.Id, m.MemberId, m.PlanId, m.PriceAtPurchase, m.StartDate, m.EndDate,
        m.Status, m.FrozenFrom, m.FrozenUntil);
}
