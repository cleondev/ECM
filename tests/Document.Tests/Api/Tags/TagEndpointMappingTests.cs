using ECM.Document.Api.Tags;

using Xunit;

namespace Document.Tests.Api.Tags;

public class TagEndpointMappingTests
{
    [Theory]
    [InlineData("file", TagEndpointMapping.ManagementDefaultIconKey)]
    [InlineData("FILE", TagEndpointMapping.ManagementDefaultIconKey)]
    [InlineData("folder", TagEndpointMapping.ManagementDefaultIconKey)]
    [InlineData("tag", TagEndpointMapping.UserDefaultIconKey)]
    [InlineData(" label ", TagEndpointMapping.UserDefaultIconKey)]
    [InlineData("briefcase", "💼")]
    [InlineData("rocket", "🚀")]
    [InlineData("lab", "🧫")]
    [InlineData("music", "🎼")]
    [InlineData("📁", "📁")]
    [InlineData("🏷️", "🏷️")]
    public void NormalizeIcon_ResolvesAliasesAndEmoji(string iconKey, string expected)
    {
        var normalized = TagEndpointMapping.NormalizeIcon(iconKey, TagEndpointMapping.UserDefaultIconKey);

        Assert.Equal(expected, normalized);
    }

    [Fact]
    public void NormalizeIcon_WhenEmpty_ReturnsDefault()
    {
        var normalized = TagEndpointMapping.NormalizeIcon("   ", TagEndpointMapping.ManagementDefaultIconKey);

        Assert.Equal(TagEndpointMapping.ManagementDefaultIconKey, normalized);
    }

    [Fact]
    public void NormalizeIcon_WhenUnsupported_ReturnsDefault()
    {
        var normalized = TagEndpointMapping.NormalizeIcon("unknown-icon", TagEndpointMapping.UserDefaultIconKey);

        Assert.Equal(TagEndpointMapping.UserDefaultIconKey, normalized);
    }
}
