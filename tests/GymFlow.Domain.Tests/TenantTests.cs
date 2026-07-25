using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests;

public class TenantTests
{
    [Fact]
    public void Constructor_NormalizaSubdominioYAsignaTrial()
    {
        var tenant = new Tenant("Gimnasio ACME", "ACME");

        Assert.Equal("acme", tenant.Subdomain);
        Assert.Equal("America/Lima", tenant.TimeZoneId);
        Assert.Equal(TenantSubscriptionStatus.Trial, tenant.SubscriptionStatus);
        Assert.True(tenant.IsActive);
    }

    [Fact]
    public void Constructor_SubdominioVacio_Lanza()
    {
        Assert.Throws<ArgumentException>(() => new Tenant("ACME", ""));
    }

    [Fact]
    public void SetSubscriptionStatus_Suspendido_NoActivo()
    {
        var tenant = new Tenant("ACME", "acme");

        tenant.SetSubscriptionStatus(TenantSubscriptionStatus.Suspended);
        Assert.False(tenant.IsActive);
    }
}
