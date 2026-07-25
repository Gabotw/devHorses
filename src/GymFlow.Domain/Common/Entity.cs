namespace GymFlow.Domain.Common;

/// <summary>
/// Base de toda entidad del dominio. Identidad por Guid generado en el dominio
/// (no depende de la base de datos para asignar el Id).
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>Instante de creación (UTC). Las fechas viven en UTC en la DB.</summary>
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; protected set; }

    protected void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
