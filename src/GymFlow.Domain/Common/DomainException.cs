namespace GymFlow.Domain.Common;

/// <summary>
/// Violación de una regla de negocio. La API la traduce a 400/409 según el caso.
/// Se usa para invariantes del dominio, no para validación de formato (eso va en Application).
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
