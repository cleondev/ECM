using ECM.Modules.Document.Domain.Documents;
using Xunit;

namespace Document.Tests.Domain.Documents;

public class DocumentTitleTests
{
    [Fact]
    public void Create_WithValidValue_TrimsWhitespace()
    {
        var title = DocumentTitle.Create("  Quarterly Report  ");

        Assert.Equal("Quarterly Report", title.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingValue_ThrowsArgumentException(string? value)
    {
        Assert.Throws<ArgumentException>(() => DocumentTitle.Create(value));
    }
}
