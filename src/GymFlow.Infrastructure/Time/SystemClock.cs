using GymFlow.Application.Abstractions.Time;

namespace GymFlow.Infrastructure.Time;

/// <summary>Reloj real del sistema. Resuelve zonas horarias IANA (p.ej. "America/Lima").</summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly TodayIn(string timeZoneId)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Resolve(timeZoneId));
        return DateOnly.FromDateTime(local);
    }

    public DateTime ToLocalTime(DateTime utc, string timeZoneId)
    {
        var utcKind = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utcKind, Resolve(timeZoneId));
    }

    public DateTime StartOfDayUtc(DateOnly localDate, string timeZoneId)
    {
        var localMidnight = localDate.ToDateTime(TimeOnly.MinValue); // DateTimeKind.Unspecified
        return TimeZoneInfo.ConvertTimeToUtc(localMidnight, Resolve(timeZoneId));
    }

    private static TimeZoneInfo Resolve(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
