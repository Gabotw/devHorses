namespace GymFlow.Application.Features.Staff;

public interface IStaffService
{
    Task<IReadOnlyList<StaffUserDto>> ListAsync(CancellationToken ct = default);

    Task<StaffUserDto> CreateAsync(CreateStaffRequest request, CancellationToken ct = default);

    /// <summary>Actualiza nombre y rol del usuario.</summary>
    Task<StaffUserDto> UpdateAsync(Guid id, UpdateStaffRequest request, CancellationToken ct = default);

    Task ResetPasswordAsync(Guid id, ResetStaffPasswordRequest request, CancellationToken ct = default);

    Task ActivateAsync(Guid id, CancellationToken ct = default);

    Task DeactivateAsync(Guid id, CancellationToken ct = default);
}
