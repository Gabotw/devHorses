using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.Features.Staff;

/// <summary>Alta de un usuario del panel dentro del tenant.</summary>
public sealed record CreateStaffRequest(string FullName, string Email, string Password, StaffRole Role);

/// <summary>Edición de nombre y rol. El correo no se cambia (identidad de login).</summary>
public sealed record UpdateStaffRequest(string FullName, StaffRole Role);

public sealed record ResetStaffPasswordRequest(string Password);

public sealed record StaffUserDto(
    Guid Id,
    string FullName,
    string Email,
    StaffRole Role,
    bool IsActive,
    DateTime? LastLoginAtUtc,
    DateTime CreatedAtUtc)
{
    public static StaffUserDto From(StaffUser u) => new(
        u.Id, u.FullName, u.Email, u.Role, u.IsActive, u.LastLoginAtUtc, u.CreatedAtUtc);
}
