using GymFlow.Domain.Common;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests;

public class MembershipTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static (Member member, MembershipPlan plan) NewMemberAndPlan(int durationDays = 30, decimal price = 100m)
    {
        var member = new Member(TenantId, "Ana Pérez", "12345678");
        var plan = new MembershipPlan(TenantId, "Mensual", price, durationDays);
        return (member, plan);
    }

    [Fact]
    public void Constructor_CalculaFinYSnapshotDePrecio()
    {
        var (member, plan) = NewMemberAndPlan(durationDays: 30, price: 120m);
        var start = new DateOnly(2026, 1, 1);

        var m = new Membership(TenantId, member, plan, start);

        Assert.Equal(new DateOnly(2026, 1, 31), m.EndDate);
        Assert.Equal(120m, m.PriceAtPurchase);
        Assert.Equal(30, m.DurationDaysAtPurchase);
        Assert.Equal(MembershipStatus.Active, m.Status);
    }

    [Fact]
    public void Constructor_MemberYPlanDeDistintoTenant_Lanza()
    {
        var member = new Member(TenantId, "Ana", "111");
        var otroPlan = new MembershipPlan(Guid.NewGuid(), "Mensual", 100m, 30);

        Assert.Throws<DomainException>(() =>
            new Membership(TenantId, member, otroPlan, new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void Freeze_ExtiendeVencimientoPorLosDiasCongelados()
    {
        var (member, plan) = NewMemberAndPlan(durationDays: 30);
        var m = new Membership(TenantId, member, plan, new DateOnly(2026, 1, 1)); // fin 01-31

        m.Freeze(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 20)); // 10 días

        Assert.Equal(MembershipStatus.Frozen, m.Status);
        Assert.Equal(new DateOnly(2026, 2, 10), m.EndDate); // 31 + 10
    }

    [Fact]
    public void Freeze_NoActiva_Lanza()
    {
        var (member, plan) = NewMemberAndPlan();
        var m = new Membership(TenantId, member, plan, new DateOnly(2026, 1, 1));
        m.Freeze(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 20));

        Assert.Throws<DomainException>(() =>
            m.Freeze(new DateOnly(2026, 1, 25), new DateOnly(2026, 1, 28)));
    }

    [Fact]
    public void Unfreeze_ReanudaAntes_DevuelveDiasNoUsados()
    {
        var (member, plan) = NewMemberAndPlan(durationDays: 30);
        var m = new Membership(TenantId, member, plan, new DateOnly(2026, 1, 1)); // fin 01-31
        m.Freeze(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 20)); // fin 02-10

        m.Unfreeze(new DateOnly(2026, 1, 15)); // reanuda 5 días antes de 01-20

        Assert.Equal(MembershipStatus.Active, m.Status);
        Assert.Null(m.FrozenFrom);
        Assert.Equal(new DateOnly(2026, 2, 5), m.EndDate); // 02-10 - 5
    }

    [Fact]
    public void RecalculateStatus_PasadoElFin_MarcaVencida()
    {
        var (member, plan) = NewMemberAndPlan(durationDays: 30);
        var m = new Membership(TenantId, member, plan, new DateOnly(2026, 1, 1)); // fin 01-31

        m.RecalculateStatus(new DateOnly(2026, 2, 1));

        Assert.Equal(MembershipStatus.Expired, m.Status);
    }

    [Fact]
    public void IsActiveOn_DentroDelPeriodo_EsVerdadero()
    {
        var (member, plan) = NewMemberAndPlan(durationDays: 30);
        var m = new Membership(TenantId, member, plan, new DateOnly(2026, 1, 1));

        Assert.True(m.IsActiveOn(new DateOnly(2026, 1, 15)));
        Assert.False(m.IsActiveOn(new DateOnly(2026, 2, 15)));
    }
}
