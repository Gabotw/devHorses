namespace GymFlow.Application.Common;

/// <summary>Recurso inexistente dentro del tenant actual. La API la traduce a 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);
