using GymFlow.Domain.Common;
using GymFlow.Domain.Entities;

namespace GymFlow.Domain.Tests;

public class MemberTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static Member NewMember() => new(TenantId, "Juan Pérez", "12345678");

    [Fact]
    public void MiembroNuevo_NoTieneAccesoAApp()
    {
        var member = NewMember();

        Assert.False(member.HasAppAccess);
        Assert.Null(member.PasswordHash);
    }

    [Fact]
    public void SetPasswordHash_HabilitaAccesoAApp()
    {
        var member = NewMember();

        member.SetPasswordHash("$2a$11$hashficticio");

        Assert.True(member.HasAppAccess);
        Assert.Equal("$2a$11$hashficticio", member.PasswordHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetPasswordHash_Vacio_Lanza(string hash)
    {
        var member = NewMember();

        Assert.Throws<DomainException>(() => member.SetPasswordHash(hash));
    }

    [Fact]
    public void RegisterLogin_FijaUltimoAcceso()
    {
        var member = NewMember();
        Assert.Null(member.LastLoginAtUtc);

        member.RegisterLogin();

        Assert.NotNull(member.LastLoginAtUtc);
    }
}
