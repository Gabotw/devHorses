using GymFlow.Domain.Common;
using GymFlow.Domain.Entities;

namespace GymFlow.Domain.Tests;

public class MembershipPlanTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void SetPrice_Redondea2Decimales()
    {
        var plan = new MembershipPlan(TenantId, "Mensual", 99.999m, 30);
        Assert.Equal(100.00m, plan.Price);
    }

    [Fact]
    public void SetPrice_Negativo_Lanza()
    {
        var plan = new MembershipPlan(TenantId, "Mensual", 100m, 30);
        Assert.Throws<DomainException>(() => plan.SetPrice(-1m));
    }

    [Fact]
    public void SetDuration_CeroODias_Lanza()
    {
        Assert.Throws<DomainException>(() => new MembershipPlan(TenantId, "X", 100m, 0));
    }

    [Fact]
    public void MonthlyAccesses_NullEsIlimitado()
    {
        var plan = new MembershipPlan(TenantId, "Ilimitado", 150m, 30, monthlyAccesses: null);
        Assert.Null(plan.MonthlyAccesses);
    }
}
