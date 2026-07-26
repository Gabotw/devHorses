namespace GymFlow.Domain.Enums;

/// <summary>Canal por el que se registró el check-in.</summary>
public enum CheckInMethod
{
    /// <summary>Registrado por el staff en recepción (web).</summary>
    Reception = 1,

    /// <summary>Registrado por el propio miembro desde la app (Fase 4).</summary>
    App = 2,
}
