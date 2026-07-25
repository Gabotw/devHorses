using GymFlow.Domain.Common;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

/// <summary>
/// Un gimnasio. Es la raíz de la multi-tenancy: NO implementa ITenantScoped
/// porque es el tenant mismo. Su Id es el TenantId que llevan las demás entidades.
/// </summary>
public class Tenant : Entity
{
    // EF Core
    private Tenant() { }

    public Tenant(string name, string subdomain, string timeZoneId = "America/Lima")
    {
        Rename(name);
        SetSubdomain(subdomain);
        TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? "America/Lima" : timeZoneId;
        SubscriptionStatus = TenantSubscriptionStatus.Trial;
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Subdominio único de resolución de tenant, p.ej. "acme" en acme.gymflow.pe.</summary>
    public string Subdomain { get; private set; } = string.Empty;

    /// <summary>Zona horaria IANA del tenant. Fechas se guardan UTC y se muestran aquí.</summary>
    public string TimeZoneId { get; private set; } = "America/Lima";

    public TenantSubscriptionStatus SubscriptionStatus { get; private set; }

    public bool IsActive => SubscriptionStatus is TenantSubscriptionStatus.Trial or TenantSubscriptionStatus.Active;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del gimnasio es obligatorio.", nameof(name));
        Name = name.Trim();
        Touch();
    }

    public void SetSubdomain(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            throw new ArgumentException("El subdominio es obligatorio.", nameof(subdomain));
        Subdomain = subdomain.Trim().ToLowerInvariant();
        Touch();
    }

    public void SetSubscriptionStatus(TenantSubscriptionStatus status)
    {
        SubscriptionStatus = status;
        Touch();
    }
}
