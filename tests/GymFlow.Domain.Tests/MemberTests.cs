using GymFlow.Domain.Common;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests;

public class MemberTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static Member NewMember() => new(TenantId, "Juan Pérez", "12345678");

    [Fact]
    public void MiembroNuevo_NaceActivo()
    {
        var member = NewMember();

        Assert.Equal(MemberStatus.Active, member.Status);
        Assert.Equal("Juan Pérez", member.FullName);
        Assert.Equal("12345678", member.DocumentId);
    }

    [Fact]
    public void Deactivate_MarcaInactivo_YActivateLoRevierte()
    {
        var member = NewMember();

        member.Deactivate();
        Assert.Equal(MemberStatus.Inactive, member.Status);

        member.Activate();
        Assert.Equal(MemberStatus.Active, member.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetFullName_Vacio_Lanza(string name)
    {
        var member = NewMember();

        Assert.Throws<DomainException>(() => member.SetFullName(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetDocumentId_Vacio_Lanza(string doc)
    {
        var member = NewMember();

        Assert.Throws<DomainException>(() => member.SetDocumentId(doc));
    }
}
