using GymFlow.Domain.Common;

namespace GymFlow.Domain.Entities;

/// <summary>
/// Plan de membresía que ofrece el gimnasio (p.ej. "Mensual", "Trimestral").
/// El precio es SIEMPRE decimal. La duración se expresa en días.
/// </summary>
public class MembershipPlan : Entity, ITenantScoped
{
    // EF Core
    private MembershipPlan() { }

    public MembershipPlan(
        Guid tenantId,
        string name,
        decimal price,
        int durationDays,
        int? monthlyAccesses = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId es obligatorio.", nameof(tenantId));

        TenantId = tenantId;
        SetName(name);
        SetPrice(price);
        SetDuration(durationDays);
        SetMonthlyAccesses(monthlyAccesses);
        IsActive = true;
    }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Precio del plan. Decimal, nunca float/double.</summary>
    public decimal Price { get; private set; }

    /// <summary>Duración de la membresía en días (p.ej. 30).</summary>
    public int DurationDays { get; private set; }

    /// <summary>Accesos por mes; null = ilimitado.</summary>
    public int? MonthlyAccesses { get; private set; }

    public bool IsActive { get; private set; }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del plan es obligatorio.");
        Name = name.Trim();
        Touch();
    }

    public void SetPrice(decimal price)
    {
        if (price < 0)
            throw new DomainException("El precio no puede ser negativo.");
        Price = decimal.Round(price, 2);
        Touch();
    }

    public void SetDuration(int durationDays)
    {
        if (durationDays <= 0)
            throw new DomainException("La duración debe ser de al menos 1 día.");
        DurationDays = durationDays;
        Touch();
    }

    public void SetMonthlyAccesses(int? monthlyAccesses)
    {
        if (monthlyAccesses is < 0)
            throw new DomainException("Los accesos por mes no pueden ser negativos.");
        MonthlyAccesses = monthlyAccesses;
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
