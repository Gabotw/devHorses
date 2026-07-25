using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests;

public class StaffUserTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Constructor_NormalizaEmailAMinusculas()
    {
        var user = new StaffUser(TenantId, "Ana Pérez", "Ana@Demo.PE", "hash", StaffRole.Admin);

        Assert.Equal("ana@demo.pe", user.Email);
        Assert.Equal(TenantId, user.TenantId);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void Constructor_SinTenant_Lanza()
    {
        Assert.Throws<ArgumentException>(() =>
            new StaffUser(Guid.Empty, "Ana", "ana@demo.pe", "hash", StaffRole.Owner));
    }

    [Fact]
    public void RegisterLogin_FijaLastLogin()
    {
        var user = new StaffUser(TenantId, "Ana", "ana@demo.pe", "hash", StaffRole.Reception);

        Assert.Null(user.LastLoginAtUtc);
        user.RegisterLogin();
        Assert.NotNull(user.LastLoginAtUtc);
    }

    [Fact]
    public void Deactivate_MarcaInactivo()
    {
        var user = new StaffUser(TenantId, "Ana", "ana@demo.pe", "hash", StaffRole.Admin);

        user.Deactivate();
        Assert.False(user.IsActive);
    }
}
