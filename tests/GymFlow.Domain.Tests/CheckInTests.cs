using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests;

public class CheckInTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid MemberId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 3, 10);

    [Fact]
    public void Valid_MarcaValidoSinMotivo()
    {
        var occurred = new DateTime(2026, 3, 10, 8, 30, 0, DateTimeKind.Utc);

        var checkIn = CheckIn.Valid(TenantId, MemberId, CheckInMethod.Reception, occurred, Today);

        Assert.True(checkIn.IsValid);
        Assert.Null(checkIn.Reason);
        Assert.Equal(CheckInMethod.Reception, checkIn.Method);
        Assert.Equal(Today, checkIn.LocalDate);
        Assert.Equal(DateTimeKind.Utc, checkIn.OccurredAtUtc.Kind);
    }

    [Fact]
    public void Rejected_MarcaNoValidoConMotivo()
    {
        var checkIn = CheckIn.Rejected(
            TenantId, MemberId, CheckInMethod.Reception, DateTime.UtcNow, Today, "Sin membresía vigente.");

        Assert.False(checkIn.IsValid);
        Assert.Equal("Sin membresía vigente.", checkIn.Reason);
    }

    [Fact]
    public void Rejected_MotivoVacio_UsaTextoPorDefecto()
    {
        var checkIn = CheckIn.Rejected(
            TenantId, MemberId, CheckInMethod.App, DateTime.UtcNow, Today, "   ");

        Assert.False(checkIn.IsValid);
        Assert.False(string.IsNullOrWhiteSpace(checkIn.Reason));
    }

    [Fact]
    public void Valid_TenantVacio_Lanza()
    {
        Assert.Throws<ArgumentException>(() =>
            CheckIn.Valid(Guid.Empty, MemberId, CheckInMethod.Reception, DateTime.UtcNow, Today));
    }

    [Fact]
    public void Valid_MemberVacio_Lanza()
    {
        Assert.Throws<ArgumentException>(() =>
            CheckIn.Valid(TenantId, Guid.Empty, CheckInMethod.Reception, DateTime.UtcNow, Today));
    }
}
