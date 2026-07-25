using GymFlow.Domain.Common;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

/// <summary>
/// Usuario del panel (recepción/admin/owner) que pertenece a un tenant.
/// Se autentica con email + contraseña (hash BCrypt). El login valida
/// siempre contra el tenant resuelto; nunca se confía en el TenantId del cliente.
/// </summary>
public class StaffUser : Entity, ITenantScoped
{
    // EF Core
    private StaffUser() { }

    public StaffUser(Guid tenantId, string fullName, string email, string passwordHash, StaffRole role)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId es obligatorio.", nameof(tenantId));

        TenantId = tenantId;
        SetFullName(fullName);
        SetEmail(email);
        SetPasswordHash(passwordHash);
        Role = role;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    /// <summary>Email en minúsculas. Único por tenant (dos tenants pueden repetir email).</summary>
    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public StaffRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    public void SetFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("El nombre es obligatorio.", nameof(fullName));
        FullName = fullName.Trim();
        Touch();
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El email es obligatorio.", nameof(email));
        Email = email.Trim().ToLowerInvariant();
        Touch();
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("El hash de contraseña es obligatorio.", nameof(passwordHash));
        PasswordHash = passwordHash;
        Touch();
    }

    public void ChangeRole(StaffRole role)
    {
        Role = role;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    public void RegisterLogin()
    {
        LastLoginAtUtc = DateTime.UtcNow;
        Touch();
    }
}
