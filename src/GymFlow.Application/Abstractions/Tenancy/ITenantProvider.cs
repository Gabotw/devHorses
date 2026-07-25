namespace GymFlow.Application.Abstractions.Tenancy;

/// <summary>
/// Puerto que expone el tenant de la request actual (scoped).
/// Lo resuelve el middleware de la API y lo consume EF Core para el filtro global.
/// Nunca se confía en un TenantId que venga del cliente sin validarlo aquí.
/// </summary>
public interface ITenantProvider
{
    /// <summary>Tenant resuelto para la request, o null si aún no se resolvió (p.ej. endpoints públicos).</summary>
    Guid? TenantId { get; }

    bool HasTenant { get; }

    /// <summary>Obtiene el TenantId o lanza si no hay tenant en contexto.</summary>
    Guid GetRequiredTenantId();

    /// <summary>Fija el tenant de la request. Lo llama el middleware una sola vez.</summary>
    void SetTenant(Guid tenantId);
}
