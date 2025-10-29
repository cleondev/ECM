using ECM.File.Application.Shares;
using ECM.File.Domain.Shares;
using Xunit;

namespace File.Tests.Application.Shares;

public class ShareLinkMapperTests
{
    [Fact]
    public void ToDto_WithoutBaseUrl_ReturnsRelativeShareUrlWithQuery()
    {
        var share = CreateShareLink("ABC123");
        var options = new ShareLinkOptions { PublicBaseUrl = null };

        var dto = ShareLinkMapper.ToDto(share, options);

        Assert.Equal("/s/?code=ABC123", dto.Url);
    }

    [Fact]
    public void ToDto_WithBaseUrl_AppendsQueryParameterAndEncodesCode()
    {
        var share = CreateShareLink("A+B ");
        var options = new ShareLinkOptions { PublicBaseUrl = "https://files.example.com/portal/" };

        var dto = ShareLinkMapper.ToDto(share, options);

        Assert.Equal("https://files.example.com/portal/s/?code=A%2BB", dto.Url);
    }

    private static ShareLink CreateShareLink(string code)
    {
        var now = DateTimeOffset.UtcNow;

        return new ShareLink(
            Guid.NewGuid(),
            code,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ShareSubjectType.Public,
            null,
            SharePermission.View | SharePermission.Download,
            null,
            now.AddMinutes(-5),
            now.AddMinutes(55),
            10,
            20,
            "example.pdf",
            "pdf",
            "application/pdf",
            1024,
            now.AddDays(-1),
            null,
            null,
            now.AddMinutes(-10),
            null);
    }
}
