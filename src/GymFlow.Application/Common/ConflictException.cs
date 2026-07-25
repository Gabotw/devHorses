namespace GymFlow.Application.Common;

/// <summary>Conflicto de estado o unicidad (p.ej. documento duplicado). La API la traduce a 409.</summary>
public sealed class ConflictException(string message) : Exception(message);
