using System;
using System.Text.RegularExpressions;
using ECM.File.Application.Files;
using Xunit;

namespace File.Tests.Application.Files;

public class DefaultStorageKeyGeneratorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Generate_WithMissingFileName_ThrowsArgumentException(string? fileName)
    {
        var generator = new DefaultStorageKeyGenerator();

        Assert.Throws<ArgumentException>(() => generator.Generate(fileName!));
    }

    [Fact]
    public void Generate_WithExtension_NormalizesExtensionToLowercase()
    {
        var generator = new DefaultStorageKeyGenerator();

        var key = generator.Generate("Quarterly-Report.PDF");

        Assert.EndsWith(".pdf", key);
        Assert.Matches(new Regex("^[0-9a-f]{32}\\.pdf$", RegexOptions.IgnoreCase), key);
    }

    [Fact]
    public void Generate_WithoutExtension_ReturnsBareIdentifier()
    {
        var generator = new DefaultStorageKeyGenerator();

        var key = generator.Generate("notes");

        Assert.Matches("^[0-9a-f]{32}$", key);
    }
}
