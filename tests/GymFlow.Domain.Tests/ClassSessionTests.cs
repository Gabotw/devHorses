using GymFlow.Domain.Common;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests;

public class ClassSessionTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static ClassSession NewSession(int capacity = 10, int durationMinutes = 60)
    {
        var startsAt = new DateTime(2026, 8, 1, 19, 0, 0, DateTimeKind.Utc);
        return new ClassSession(TenantId, "Yoga", "Ana", startsAt, new DateOnly(2026, 8, 1), durationMinutes, capacity);
    }

    [Fact]
    public void Constructor_CalculaFinYNormalizaUtc()
    {
        var s = NewSession(durationMinutes: 45);

        Assert.Equal(ClassSessionStatus.Scheduled, s.Status);
        Assert.Equal(DateTimeKind.Utc, s.StartsAtUtc.Kind);
        Assert.Equal(new DateTime(2026, 8, 1, 19, 45, 0, DateTimeKind.Utc), s.EndsAtUtc);
    }

    [Fact]
    public void SetCapacity_CeroOMenos_Lanza()
    {
        var s = NewSession();
        Assert.Throws<DomainException>(() => s.SetCapacity(0));
    }

    [Fact]
    public void SetDuration_CeroOMenos_Lanza()
    {
        var s = NewSession();
        Assert.Throws<DomainException>(() => s.SetDuration(0));
    }

    [Fact]
    public void Cancel_MarcaCancelada()
    {
        var s = NewSession();
        s.Cancel();
        Assert.True(s.IsCancelled);
        Assert.Equal(ClassSessionStatus.Cancelled, s.Status);
    }
}
