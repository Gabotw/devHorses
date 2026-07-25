using GymFlow.Application.Abstractions.Tenancy;

namespace GymFlow.Api.Multitenancy;

/// <summary>
/// Portador scoped del tenant de la request. Lo fija el middleware una sola vez
/// y lo consume el AppDbContext para el filtro global.
/// </summary>
public sealed class TenantProvider : ITenantProvider
{
    private Guid? _tenantId;

    public Guid? TenantId => _tenantId;

    public bool HasTenant => _tenantId.HasValue;

    public Guid GetRequiredTenantId() =>
        _tenantId ?? throw new InvalidOperationException("No hay tenant resuelto en la request.");

    public void SetTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId inválido.", nameof(tenantId));
        _tenantId = tenantId;
    }
}
