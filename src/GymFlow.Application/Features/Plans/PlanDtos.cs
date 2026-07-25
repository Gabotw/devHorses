using GymFlow.Domain.Entities;

namespace GymFlow.Application.Features.Plans;

public sealed record CreatePlanRequest(
    string Name,
    decimal Price,
    int DurationDays,
    int? MonthlyAccesses);

public sealed record UpdatePlanRequest(
    string Name,
    decimal Price,
    int DurationDays,
    int? MonthlyAccesses);

public sealed record PlanDto(
    Guid Id,
    string Name,
    decimal Price,
    int DurationDays,
    int? MonthlyAccesses,
    bool IsActive)
{
    public static PlanDto From(MembershipPlan p) =>
        new(p.Id, p.Name, p.Price, p.DurationDays, p.MonthlyAccesses, p.IsActive);
}
