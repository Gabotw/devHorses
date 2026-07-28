namespace GymFlow.Application.Abstractions.Time;

/// <summary>Reloj inyectable (testeable). Siempre trabaja en UTC.</summary>
public interface IClock
{
    DateTime UtcNow { get; }

    /// <summary>Fecha de hoy en la zona horaria IANA indicada (p.ej. "America/Lima").</summary>
    DateOnly TodayIn(string timeZoneId);

    /// <summary>Convierte un instante UTC a la hora local de la zona indicada (para agrupar reportes por día/hora).</summary>
    DateTime ToLocalTime(DateTime utc, string timeZoneId);

    /// <summary>Instante UTC correspondiente a la medianoche (inicio) de una fecha local en la zona indicada.</summary>
    DateTime StartOfDayUtc(DateOnly localDate, string timeZoneId);
}
