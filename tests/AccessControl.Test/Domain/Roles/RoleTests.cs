using ECM.AccessControl.Domain.Roles;
using Xunit;

namespace AccessControl.Test.Domain.Roles;

public class RoleTests
{
    [Fact]
    public void Create_WithValidName_TrimsValueAndGeneratesId()
    {
        var role = Role.Create("  Administrator  ");

        Assert.NotEqual(Guid.Empty, role.Id);
        Assert.Equal("Administrator", role.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingName_ThrowsArgumentException(string? name)
    {
        var exception = Assert.Throws<ArgumentException>(() => Role.Create(name!));
        Assert.Equal("Role name is required.", exception.Message);
        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Rename_WithValidValue_UpdatesName()
    {
        var role = Role.Create("User");

        role.Rename("  Editors  ");

        Assert.Equal("Editors", role.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithMissingName_ThrowsArgumentException(string? name)
    {
        var role = Role.Create("Auditor");

        var exception = Assert.Throws<ArgumentException>(() => role.Rename(name!));
        Assert.Equal("Role name is required.", exception.Message);
        Assert.Equal("name", exception.ParamName);
    }
}
