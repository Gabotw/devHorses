using GymFlow.Domain.Common;

namespace GymFlow.Domain.Entities;

/// <summary>
/// Plan de la PLATAFORMA al que se suscribe un gimnasio (billing SaaS, Fase 6). Es de
/// nivel plataforma: NO implementa <see cref="ITenantScoped"/> (lo administra el super-admin,
/// no vive dentro de un tenant). El precio es SIEMPRE decimal; el período de facturación se
/// expresa en días (30 = mensual). <see cref="MaxMembers"/> null = sin límite de miembros.
/// </summary>
public class PlatformPlan : Entity
{
    // EF Core
    private PlatformPlan() { }

    public PlatformPlan(string name, decimal monthlyPrice, int billingPeriodDays = 30, int? maxMembers = null)
    {
        SetName(name);
        SetMonthlyPrice(monthlyPrice);
        SetBillingPeriodDays(billingPeriodDays);
        SetMaxMembers(maxMembers);
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Precio del período de facturación. Decimal, nunca float/double.</summary>
    public decimal MonthlyPrice { get; private set; }

    /// <summary>Días que dura cada período de facturación (30 = mensual, 365 = anual).</summary>
    public int BillingPeriodDays { get; private set; }

    /// <summary>Tope de miembros que el plan permite al gimnasio; null = ilimitado.</summary>
    public int? MaxMembers { get; private set; }

    public bool IsActive { get; private set; }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del plan de plataforma es obligatorio.");
        Name = name.Trim();
        Touch();
    }

    public void SetMonthlyPrice(decimal monthlyPrice)
    {
        if (monthlyPrice < 0)
            throw new DomainException("El precio no puede ser negativo.");
        MonthlyPrice = decimal.Round(monthlyPrice, 2, MidpointRounding.AwayFromZero);
        Touch();
    }

    public void SetBillingPeriodDays(int billingPeriodDays)
    {
        if (billingPeriodDays <= 0)
            throw new DomainException("El período de facturación debe ser de al menos 1 día.");
        BillingPeriodDays = billingPeriodDays;
        Touch();
    }

    public void SetMaxMembers(int? maxMembers)
    {
        if (maxMembers is <= 0)
            throw new DomainException("El tope de miembros debe ser mayor a cero (o null para ilimitado).");
        MaxMembers = maxMembers;
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}
