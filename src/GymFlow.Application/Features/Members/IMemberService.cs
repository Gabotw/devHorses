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

    /// <summary>Asigna/renueva la contraseña de acceso a la app del miembro (acción de staff).</summary>
    Task SetPasswordAsync(Guid id, SetMemberPasswordRequest request, CancellationToken ct = default);
}
