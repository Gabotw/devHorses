using GymFlow.Domain.Common;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests;

public class ClassReservationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid SessionId = Guid.NewGuid();
    private static readonly Guid MemberId = Guid.NewGuid();

    [Fact]
    public void Booked_NaceConCupoYActiva()
    {
        var r = ClassReservation.Booked(TenantId, SessionId, MemberId);
        Assert.Equal(ClassReservationStatus.Booked, r.Status);
        Assert.True(r.IsActive);
    }

    [Fact]
    public void Promote_DeEsperaAConfirmada()
    {
        var r = ClassReservation.Waitlisted(TenantId, SessionId, MemberId);
        r.Promote();
        Assert.Equal(ClassReservationStatus.Booked, r.Status);
    }

    [Fact]
    public void Promote_NoEstaEnEspera_Lanza()
    {
        var r = ClassReservation.Booked(TenantId, SessionId, MemberId);
        Assert.Throws<DomainException>(() => r.Promote());
    }

    [Fact]
    public void Cancel_LiberaYGuardaInstanteUtc()
    {
        var r = ClassReservation.Booked(TenantId, SessionId, MemberId);
        r.Cancel(DateTime.UtcNow);

        Assert.Equal(ClassReservationStatus.Cancelled, r.Status);
        Assert.False(r.IsActive);
        Assert.NotNull(r.CanceledAtUtc);
        Assert.Equal(DateTimeKind.Utc, r.CanceledAtUtc!.Value.Kind);
    }

    [Fact]
    public void Cancel_YaCancelada_Lanza()
    {
        var r = ClassReservation.Booked(TenantId, SessionId, MemberId);
        r.Cancel(DateTime.UtcNow);
        Assert.Throws<DomainException>(() => r.Cancel(DateTime.UtcNow));
    }

    [Fact]
    public void MarkAttended_DesdeCupo_MarcaAsistio()
    {
        var r = ClassReservation.Booked(TenantId, SessionId, MemberId);
        r.MarkAttended(DateTime.UtcNow);

        Assert.Equal(ClassReservationStatus.Attended, r.Status);
        Assert.NotNull(r.AttendedAtUtc);
    }

    [Fact]
    public void MarkAttended_EnEspera_Lanza()
    {
        var r = ClassReservation.Waitlisted(TenantId, SessionId, MemberId);
        Assert.Throws<DomainException>(() => r.MarkAttended(DateTime.UtcNow));
    }
}
