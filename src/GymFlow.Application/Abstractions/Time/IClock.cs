namespace GymFlow.Application.Abstractions.Time;

/// <summary>Reloj inyectable (testeable). Siempre trabaja en UTC.</summary>
public interface IClock
{
    DateTime UtcNow { get; }

    /// <summary>Fecha de hoy en la zona horaria IANA indicada (p.ej. "America/Lima").</summary>
    DateOnly TodayIn(string timeZoneId);
}
