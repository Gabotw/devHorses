using GymFlow.Domain.Enums;

namespace GymFlow.Application.Features.Me;

/// <summary>Membresía del miembro con el nombre del plan (lo que muestra la app).</summary>
public sealed record MyMembershipDto(
    Guid Id,
    string PlanName,
    decimal PriceAtPurchase,
    DateOnly StartDate,
    DateOnly EndDate,
    MembershipStatus Status);
