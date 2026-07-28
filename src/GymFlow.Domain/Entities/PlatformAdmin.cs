using GymFlow.Domain.Common;

namespace GymFlow.Domain.Entities;

/// <summary>
/// Administrador de la PLATAFORMA (super-admin del SaaS, Fase 6). Opera a través de todos
/// los tenants: gestiona los planes de plataforma y la suscripción de cada gimnasio. NO es
/// staff de un gimnasio (no tiene TenantId) y su token lleva <c>actor=platform</c> (sin
/// tenant_id), por lo que no accede a los endpoints por-tenant y viceversa.
/// </summary>
public class PlatformAdmin : Entity
{
    // EF Core
    private PlatformAdmin() { }

    public PlatformAdmin(string fullName, string email, string passwordHash)
    {
        SetFullName(fullName);
        SetEmail(email);
        SetPasswordHash(passwordHash);
        IsActive = true;
    }

    public string FullName { get; private set; } = string.Empty;

    /// <summary>Email en minúsculas. Único a nivel plataforma.</summary>
    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

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

    public void RegisterLogin()
    {
        LastLoginAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}
