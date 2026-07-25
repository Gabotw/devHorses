namespace GymFlow.Application.Features.Plans;

public interface IMembershipPlanService
{
    Task<IReadOnlyList<PlanDto>> ListAsync(bool includeInactive, CancellationToken ct = default);

    Task<PlanDto> GetAsync(Guid id, CancellationToken ct = default);

    Task<PlanDto> CreateAsync(CreatePlanRequest request, CancellationToken ct = default);

    Task<PlanDto> UpdateAsync(Guid id, UpdatePlanRequest request, CancellationToken ct = default);

    Task DeactivateAsync(Guid id, CancellationToken ct = default);

    Task ActivateAsync(Guid id, CancellationToken ct = default);
}
