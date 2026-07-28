using GymFlow.Domain.Common;
using GymFlow.Domain.Entities;

namespace GymFlow.Domain.Tests;

public class PlatformPlanTests
{
    [Fact]
    public void Constructor_ValoresPorDefecto_MensualActivo()
    {
        var plan = new PlatformPlan("Starter", 99m);

        Assert.Equal("Starter", plan.Name);
        Assert.Equal(99m, plan.MonthlyPrice);
        Assert.Equal(30, plan.BillingPeriodDays);
        Assert.Null(plan.MaxMembers);
        Assert.True(plan.IsActive);
    }

    [Fact]
    public void SetMonthlyPrice_Negativo_Lanza()
    {
        var plan = new PlatformPlan("Starter", 99m);
        Assert.Throws<DomainException>(() => plan.SetMonthlyPrice(-1m));
    }

    [Fact]
    public void SetBillingPeriodDays_CeroOMenos_Lanza()
    {
        var plan = new PlatformPlan("Starter", 99m);
        Assert.Throws<DomainException>(() => plan.SetBillingPeriodDays(0));
    }

    [Fact]
    public void SetMaxMembers_CeroOMenos_Lanza()
    {
        var plan = new PlatformPlan("Starter", 99m);
        Assert.Throws<DomainException>(() => plan.SetMaxMembers(0));
    }

    [Fact]
    public void Deactivate_MarcaInactivo()
    {
        var plan = new PlatformPlan("Starter", 99m);
        plan.Deactivate();
        Assert.False(plan.IsActive);
    }
}
