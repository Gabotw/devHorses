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

    [Fact]
    public void SetAccessCode_CuatroDigitos_Asigna()
    {
        var member = NewMember();

        member.SetAccessCode("0427");

        Assert.Equal("0427", member.AccessCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("12a4")]
    [InlineData(" abc")]
    public void SetAccessCode_Invalido_Lanza(string code)
    {
        var member = NewMember();

        Assert.Throws<DomainException>(() => member.SetAccessCode(code));
    }
}
