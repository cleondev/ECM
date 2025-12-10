using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using AppGateway.Api.Auth;
using AppGateway.Infrastructure.Ecm;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Syncfusion.EJ2.PdfViewer;

namespace AppGateway.Api.Controllers.Viewer;

[ApiController]
[Route("api/viewer/pdf/{versionId:guid}")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class PdfViewerController(IEcmApiClient ecmApiClient, ILogger<PdfViewerController> logger) : ControllerBase
{
    private static readonly PdfRenderer Renderer = new();

    private readonly IEcmApiClient _ecmApiClient = ecmApiClient;
    private readonly ILogger<PdfViewerController> _logger = logger;

    [HttpPost("load")]
    public async Task<IActionResult> LoadAsync(
        Guid versionId,
        [FromBody] Dictionary<string, string> jsonObject,
        CancellationToken cancellationToken)
    {
        if (jsonObject is null)
        {
            return BadRequest();
        }

        var preview = await _ecmApiClient.GetDocumentVersionPreviewAsync(versionId, cancellationToken);
        if (preview.IsForbidden)
        {
            return Forbid(authenticationSchemes: [GatewayAuthenticationSchemes.Default]);
        }

        if (preview.Payload is null)
        {
            return NotFound();
        }

        await using var stream = new MemoryStream(preview.Payload.Content);
        var response = Renderer.Load(stream, jsonObject);
        return Content(response, "application/json");
    }

    [HttpPost("render")]
    public IActionResult RenderPdfPages([FromBody] Dictionary<string, string> jsonObject)
    {
        return Content(Renderer.GetPage(jsonObject), "application/json");
    }

    [HttpPost("render-text")]
    public IActionResult RenderPdfTexts([FromBody] Dictionary<string, string> jsonObject)
    {
        return Content(Renderer.GetText(jsonObject), "application/json");
    }

    [HttpPost("render-thumbnails")]
    public IActionResult RenderThumbnails([FromBody] Dictionary<string, string> jsonObject)
    {
        return Content(Renderer.GetThumbnailImages(jsonObject), "application/json");
    }

    [HttpPost("bookmarks")]
    public IActionResult Bookmarks([FromBody] Dictionary<string, string> jsonObject)
    {
        return Content(Renderer.GetBookmarks(jsonObject), "application/json");
    }

    [HttpPost("annotations")]
    public IActionResult RenderAnnotations([FromBody] Dictionary<string, string> jsonObject)
    {
        return Content(Renderer.GetAnnotationComments(jsonObject), "application/json");
    }

    [HttpPost("print")]
    public IActionResult Print([FromBody] Dictionary<string, string> jsonObject)
    {
        return Content(Renderer.Print(jsonObject), "application/json");
    }

    [HttpPost("download")]
    public IActionResult Download([FromBody] Dictionary<string, string> jsonObject)
    {
        var document = Renderer.GetDocument(jsonObject);
        return File(document.DocumentStream, "application/pdf", document.DocumentName);
    }

    [HttpPost("unload")]
    public IActionResult Unload([FromBody] Dictionary<string, string> jsonObject)
    {
        Renderer.ClearCache(jsonObject);
        return Ok();
    }
}
