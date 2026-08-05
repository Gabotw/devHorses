using GymFlow.Application.Common;

namespace GymFlow.Application.Features.Members;

public interface IMemberService
{
    Task<PagedResult<MemberDto>> ListAsync(
        string? search, int page, int pageSize, CancellationToken ct = default);

    Task<MemberDto> GetAsync(Guid id, CancellationToken ct = default);

    Task<MemberDto> CreateAsync(CreateMemberRequest request, CancellationToken ct = default);

    Task<MemberDto> UpdateAsync(Guid id, UpdateMemberRequest request, CancellationToken ct = default);

    Task DeactivateAsync(Guid id, CancellationToken ct = default);

    Task ActivateAsync(Guid id, CancellationToken ct = default);

    /// <summary>Genera y asigna un nuevo código de acceso de 4 dígitos (único por tenant).</summary>
    Task<MemberDto> RegenerateAccessCodeAsync(Guid id, CancellationToken ct = default);
}
