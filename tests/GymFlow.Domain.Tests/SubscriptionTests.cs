using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests;

public class SubscriptionTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static PlatformPlan NewPlan(decimal price = 99m, int periodDays = 30) =>
        new("Starter", price, periodDays, maxMembers: 200);

    [Fact]
    public void Start_TomaSnapshotDelPlanYCalculaPeriodo()
    {
        var plan = NewPlan(price: 149m, periodDays: 30);
        var start = new DateOnly(2026, 1, 1);

        var sub = Subscription.Start(TenantId, plan, start);

        Assert.Equal(TenantId, sub.TenantId);
        Assert.Equal(plan.Id, sub.PlatformPlanId);
        Assert.Equal("Starter", sub.PlanName);
        Assert.Equal(149m, sub.PriceAtSubscription);
        Assert.Equal(30, sub.BillingPeriodDays);
        Assert.Equal(new DateOnly(2026, 1, 31), sub.CurrentPeriodEnd);
        Assert.Equal(TenantSubscriptionStatus.Active, sub.Status);
    }

    [Fact]
    public void ChangePlan_ActualizaSnapshotYReiniciaPeriodo()
    {
        var sub = Subscription.Start(TenantId, NewPlan(price: 99m, periodDays: 30), new DateOnly(2026, 1, 1));
        var pro = new PlatformPlan("Pro", 199m, billingPeriodDays: 365, maxMembers: null);

        sub.ChangePlan(pro, new DateOnly(2026, 2, 1));

        Assert.Equal("Pro", sub.PlanName);
        Assert.Equal(199m, sub.PriceAtSubscription);
        Assert.Equal(365, sub.BillingPeriodDays);
        Assert.Equal(new DateOnly(2026, 2, 1), sub.CurrentPeriodStart);
        Assert.Equal(new DateOnly(2027, 2, 1), sub.CurrentPeriodEnd);
        Assert.Equal(TenantSubscriptionStatus.Active, sub.Status);
    }

    [Fact]
    public void MarkPastDue_Vigente_PasaAMorosa()
    {
        var sub = Subscription.Start(TenantId, NewPlan(), new DateOnly(2026, 1, 1));

        sub.MarkPastDue();

        Assert.Equal(TenantSubscriptionStatus.PastDue, sub.Status);
    }

    [Fact]
    public void MarkPastDue_Cancelada_NoCambia()
    {
        var sub = Subscription.Start(TenantId, NewPlan(), new DateOnly(2026, 1, 1));
        sub.Cancel(DateTime.UtcNow);

        sub.MarkPastDue();

        Assert.Equal(TenantSubscriptionStatus.Cancelled, sub.Status);
    }

    [Fact]
    public void Renew_ReactivaYExtiendePeriodo()
    {
        var sub = Subscription.Start(TenantId, NewPlan(periodDays: 30), new DateOnly(2026, 1, 1));
        sub.MarkPastDue();

        sub.Renew(new DateOnly(2026, 3, 1));

        Assert.Equal(TenantSubscriptionStatus.Active, sub.Status);
        Assert.Equal(new DateOnly(2026, 3, 1), sub.CurrentPeriodStart);
        Assert.Equal(new DateOnly(2026, 3, 31), sub.CurrentPeriodEnd);
    }

    [Fact]
    public void Cancel_MarcaCanceladaYGuardaInstante()
    {
        var sub = Subscription.Start(TenantId, NewPlan(), new DateOnly(2026, 1, 1));
        var now = DateTime.UtcNow;

        sub.Cancel(now);

        Assert.Equal(TenantSubscriptionStatus.Cancelled, sub.Status);
        Assert.NotNull(sub.CanceledAtUtc);
        Assert.Equal(DateTimeKind.Utc, sub.CanceledAtUtc!.Value.Kind);
    }
}
