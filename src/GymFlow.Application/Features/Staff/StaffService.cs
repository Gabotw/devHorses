using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Abstractions.Security;
using GymFlow.Application.Abstractions.Tenancy;
using GymFlow.Application.Common;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Features.Staff;

/// <summary>
/// Gestión de usuarios del panel (staff) dentro del tenant. Las consultas van acotadas al
/// tenant por el global query filter; el TenantId de creación sale de ITenantProvider, nunca
/// del cliente. Restringido a Owner/Admin (policy Manager) en el controller.
/// </summary>
public sealed class StaffService(
    IAppDbContext db, ITenantProvider tenant, IPasswordHasher passwordHasher) : IStaffService
{
    private const int MinPasswordLength = 6;

    public async Task<IReadOnlyList<StaffUserDto>> ListAsync(CancellationToken ct = default)
        => await db.StaffUsers.AsNoTracking()
            .OrderBy(u => u.FullName)
            .Select(u => StaffUserDto.From(u))
            .ToListAsync(ct);

    public async Task<StaffUserDto> CreateAsync(CreateStaffRequest request, CancellationToken ct = default)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (email.Length == 0)
            throw new ConflictException("El correo es obligatorio.");
        ValidatePassword(request.Password);
        ValidateRole(request.Role);

        var exists = await db.StaffUsers.AnyAsync(u => u.Email == email, ct);
        if (exists)
            throw new ConflictException($"Ya existe un usuario con el correo {email}.");

        var user = new StaffUser(
            tenant.GetRequiredTenantId(),
            request.FullName,
            email,
            passwordHasher.Hash(request.Password),
            request.Role);

        db.StaffUsers.Add(user);
        await db.SaveChangesAsync(ct);
        return StaffUserDto.From(user);
    }

    public async Task<StaffUserDto> UpdateAsync(Guid id, UpdateStaffRequest request, CancellationToken ct = default)
    {
        ValidateRole(request.Role);
        var user = await GetAsync(id, ct);

        // Cambiar el rol de un owner a otro rol no puede dejar al gimnasio sin dueño activo.
        if (user.Role == StaffRole.Owner && request.Role != StaffRole.Owner)
            await EnsureNotLastActiveOwnerAsync(user, ct);

        user.SetFullName(request.FullName);
        user.ChangeRole(request.Role);
        await db.SaveChangesAsync(ct);
        return StaffUserDto.From(user);
    }

    public async Task ResetPasswordAsync(Guid id, ResetStaffPasswordRequest request, CancellationToken ct = default)
    {
        ValidatePassword(request.Password);
        var user = await GetAsync(id, ct);
        user.SetPasswordHash(passwordHasher.Hash(request.Password));
        await db.SaveChangesAsync(ct);
    }

    public async Task ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var user = await GetAsync(id, ct);
        user.Activate();
        await db.SaveChangesAsync(ct);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var user = await GetAsync(id, ct);
        if (user.Role == StaffRole.Owner)
            await EnsureNotLastActiveOwnerAsync(user, ct);
        user.Deactivate();
        await db.SaveChangesAsync(ct);
    }

    private async Task<StaffUser> GetAsync(Guid id, CancellationToken ct)
        => await db.StaffUsers.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("Usuario no encontrado.");

    private async Task EnsureNotLastActiveOwnerAsync(StaffUser owner, CancellationToken ct)
    {
        var otherActiveOwners = await db.StaffUsers.CountAsync(
            u => u.Role == StaffRole.Owner && u.IsActive && u.Id != owner.Id, ct);
        if (otherActiveOwners == 0)
            throw new ConflictException("No puedes dejar al gimnasio sin un dueño (owner) activo.");
    }

    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
            throw new ConflictException($"La contraseña debe tener al menos {MinPasswordLength} caracteres.");
    }

    private static void ValidateRole(StaffRole role)
    {
        if (!Enum.IsDefined(role))
            throw new ConflictException("Rol inválido.");
    }
}
