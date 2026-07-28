namespace GymFlow.Application.Features.Platform;

/// <summary>Catálogo de planes de la plataforma (SaaS). Solo lo administra el super-admin.</summary>
public interface IPlatformPlanService
{
    Task<IReadOnlyList<PlatformPlanDto>> ListAsync(bool includeInactive = true, CancellationToken ct = default);
    Task<PlatformPlanDto> CreateAsync(UpsertPlatformPlanRequest request, CancellationToken ct = default);
    Task<PlatformPlanDto> UpdateAsync(Guid id, UpsertPlatformPlanRequest request, CancellationToken ct = default);
    Task<PlatformPlanDto> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
}
