using System.Net;

using AppGateway.Api.Controllers.Viewer;
using AppGateway.Contracts.Documents;
using AppGateway.Infrastructure.Ecm;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Xunit;

namespace AppGateway.Api.Tests.Controllers.Viewer;

public sealed class ViewerControllerTests
{
    private readonly IEcmApiClient _ecmClient;
    private readonly ViewerController _controller;

    public ViewerControllerTests()
    {
        _ecmClient = Substitute.For<IEcmApiClient>();
        _controller = new ViewerController(_ecmClient, NullLogger<ViewerController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Theory]
    [InlineData("application/pdf", null, ViewerTypes.Pdf)]
    [InlineData("application/msword", null, ViewerTypes.Word)]
    [InlineData("application/vnd.ms-excel", null, ViewerTypes.Excel)]
    [InlineData("video/mp4", null, ViewerTypes.Video)]
    [InlineData("image/png", null, ViewerTypes.Image)]
    [InlineData(null, "document.docx", ViewerTypes.Word)]
    [InlineData(null, "spreadsheet.xlsx", ViewerTypes.Excel)]
    [InlineData(null, "picture.jpeg", ViewerTypes.Image)]
    [InlineData(null, "clip.mp4", ViewerTypes.Video)]
    [InlineData(null, "unknown.bin", ViewerTypes.Unsupported)]
    public void ViewerTypeMapper_ResolvesExpectedType(string? mimeType, string? storageKey, string expected)
    {
        var result = ViewerTypeMapper.Resolve(mimeType, storageKey);
        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetAsync_ReturnsViewerResponseWithUrls()
    {
        var versionId = Guid.NewGuid();
        var version = new DocumentVersionDto(
            versionId,
            1,
            "sample.docx",
            128,
            "application/msword",
            "hash",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        _ecmClient
            .GetDocumentVersionAsync(versionId, Arg.Any<CancellationToken>())
            .Returns(new EcmResponse<DocumentVersionDto>(HttpStatusCode.OK, version));

        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                PathBase = "/gateway"
            }
        };
        _controller.ControllerContext.HttpContext = httpContext;

        var result = await _controller.GetAsync(versionId, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ViewerResponse>().Subject;

        response.ViewerType.Should().Be(ViewerTypes.Word);
        response.PreviewUrl.Should().Be($"/gateway/api/documents/files/preview/{versionId}");
        response.DownloadUrl.Should().Be($"/gateway/api/documents/files/download/{versionId}");
        response.ThumbnailUrl.Should().Be($"/gateway/api/documents/files/thumbnails/{versionId}?w=400&h=400&fit=contain");
        response.WordViewerUrl.Should().Be($"/gateway/api/viewer/word/{versionId}");
        response.ExcelViewerUrl.Should().BeNull();
    }

    [Fact]
    public async Task GetWordViewerAsync_Forbidden_Forbids()
    {
        _ecmClient
            .GetDocumentVersionWordViewerAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new EcmResponse<DocumentFileContent?>(HttpStatusCode.Forbidden, null));

        var result = await _controller.GetWordViewerAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetExcelViewerAsync_NotFound_ReturnsNotFound()
    {
        _ecmClient
            .GetDocumentVersionExcelViewerAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new EcmResponse<DocumentFileContent?>(HttpStatusCode.NotFound, null));

        var result = await _controller.GetExcelViewerAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }
}
