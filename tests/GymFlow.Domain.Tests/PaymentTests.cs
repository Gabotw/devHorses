using GymFlow.Domain.Common;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests;

public class PaymentTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid MemberId = Guid.NewGuid();

    [Fact]
    public void RegisterCash_NaceCompletadoConFecha()
    {
        var paidAt = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

        var payment = Payment.RegisterCash(TenantId, MemberId, membershipId: null, amount: 100m, paidAt);

        Assert.Equal(PaymentMethod.Cash, payment.Method);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal(100m, payment.Amount);
        Assert.Equal(paidAt, payment.PaidAtUtc);
        Assert.Null(payment.GatewayReference);
    }

    [Fact]
    public void RegisterCash_RedondeaMontoA2Decimales()
    {
        var payment = Payment.RegisterCash(TenantId, MemberId, null, 100.125m, DateTime.UtcNow);

        Assert.Equal(100.13m, payment.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_MontoNoPositivo_Lanza(decimal amount)
    {
        Assert.Throws<DomainException>(() =>
            Payment.RegisterCash(TenantId, MemberId, null, amount, DateTime.UtcNow));
    }

    [Fact]
    public void StartGatewayCharge_NacePendiente()
    {
        var payment = Payment.StartGatewayCharge(TenantId, MemberId, null, 80m, PaymentMethod.Culqi);

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Null(payment.PaidAtUtc);
    }

    [Fact]
    public void StartGatewayCharge_ConEfectivo_Lanza()
    {
        Assert.Throws<DomainException>(() =>
            Payment.StartGatewayCharge(TenantId, MemberId, null, 80m, PaymentMethod.Cash));
    }

    [Fact]
    public void Complete_MarcaCompletadoYGuardaReferencia()
    {
        var payment = Payment.StartGatewayCharge(TenantId, MemberId, null, 80m, PaymentMethod.Culqi);
        var paidAt = new DateTime(2026, 3, 2, 9, 0, 0, DateTimeKind.Utc);

        payment.Complete("chr_test_123", paidAt);

        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal("chr_test_123", payment.GatewayReference);
        Assert.Equal(paidAt, payment.PaidAtUtc);
    }

    [Fact]
    public void Fail_MarcaFallidoConMotivo_SinFecha()
    {
        var payment = Payment.StartGatewayCharge(TenantId, MemberId, null, 80m, PaymentMethod.Culqi);

        payment.Fail("Tarjeta declinada");

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Tarjeta declinada", payment.FailureReason);
        Assert.Null(payment.PaidAtUtc);
    }
}
