namespace GymFlow.Domain.Common;

/// <summary>
/// Marca una entidad como perteneciente a un tenant (gimnasio).
/// Toda tabla del dominio de negocio implementa esto y EF Core aplica
/// el filtro global por TenantId automáticamente en cada query.
/// </summary>
public interface ITenantScoped
{
    Guid TenantId { get; }
}
