using ECM.Document.Application.Shares;
using ECM.Document.Domain.Shares;
using Xunit;

namespace Document.Tests.Application.Shares;

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

    [Fact]
    public void ToDto_WithFallbackBaseUrl_UsesFallbackWhenOptionMissing()
    {
        var share = CreateShareLink("XYZ789");
        var options = new ShareLinkOptions { PublicBaseUrl = null };

        var dto = ShareLinkMapper.ToDto(share, options, "https://docs.example.com/base");

        Assert.Equal("https://docs.example.com/base/s/?code=XYZ789", dto.Url);
        Assert.Equal("https://docs.example.com/base/s/XYZ789", dto.ShortUrl);
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
