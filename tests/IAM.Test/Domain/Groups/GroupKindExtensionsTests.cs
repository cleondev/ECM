using ECM.IAM.Domain.Groups;
using Xunit;

namespace IAM.Test.Domain.Groups;

public class GroupKindExtensionsTests
{
    [Theory]
    [InlineData(GroupKind.System, "system")]
    [InlineData(GroupKind.Unit, "unit")]
    [InlineData(GroupKind.Team, "team")]
    [InlineData(GroupKind.Temporary, "temporary")]
    [InlineData(GroupKind.Guess, "guess")]
    public void ToNormalizedString_ReturnsExpectedValue(GroupKind kind, string expected)
    {
        var normalized = kind.ToNormalizedString();

        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("system", GroupKind.System)]
    [InlineData("UNIT", GroupKind.Unit)]
    [InlineData("Team", GroupKind.Team)]
    [InlineData("temporary", GroupKind.Temporary)]
    [InlineData("normal", GroupKind.Temporary)]
    [InlineData("guess", GroupKind.Guess)]
    public void FromString_WithKnownValues_ReturnsExpectedKind(string value, GroupKind expected)
    {
        var kind = GroupKindExtensions.FromString(value);

        Assert.Equal(expected, kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromString_WithNullOrWhitespace_ReturnsTemporary(string? value)
    {
        var kind = GroupKindExtensions.FromString(value);

        Assert.Equal(GroupKind.Temporary, kind);
    }

    [Fact]
    public void FromString_WithUnknownValue_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => GroupKindExtensions.FromString("unsupported"));

        Assert.StartsWith("Unknown group kind", exception.Message);
        Assert.Equal("value", exception.ParamName);
    }
}
