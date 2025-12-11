using AppGateway.Api.Auth;
using AppGateway.Infrastructure.Ecm;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json;

using Syncfusion.EJ2.PdfViewer;

namespace AppGateway.Api.Controllers.Viewer;

[ApiController]
[Route("api/viewer/pdf/{versionId:guid}")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class PdfViewerController(
    IEcmApiClient ecmApiClient,
    IMemoryCache cache,
    ILogger<PdfViewerController> logger) : ControllerBase
{
    private readonly IEcmApiClient _ecmApiClient = ecmApiClient;
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<PdfViewerController> _logger = logger;

    private PdfRenderer CreateRenderer() => new(_cache);


    [HttpPost("Load")]
    public async Task<IActionResult> LoadAsync(
        Guid versionId,
        [FromBody] Dictionary<string, object> jsonObject,
        CancellationToken cancellationToken)
    {
        try
        {
            var preview = await _ecmApiClient.GetDocumentVersionPreviewAsync(versionId, cancellationToken);

            if (preview.IsForbidden)
                return Forbid(authenticationSchemes: [GatewayAuthenticationSchemes.Default]);

            if (preview.Payload is null)
                return NotFound();

            // ❗ DO NOT DISPOSE STREAM
            var stream = new MemoryStream(preview.Payload.Content);

            var viewer = CreateRenderer();
            var result = viewer.Load(stream, ConvertJson(jsonObject));

            return Content(JsonConvert.SerializeObject(result), "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load PDF");
            return StatusCode(500);
        }
    }

    [HttpPost("RenderPdfPages")]
    public IActionResult RenderPdfPages([FromBody] Dictionary<string, object> jsonObject)
    {
        var viewer = CreateRenderer();
        var result = viewer.GetPage(ConvertJson(jsonObject));
        return Content(JsonConvert.SerializeObject(result), "application/json");
    }

    [HttpPost("RenderPdfTexts")]
    public IActionResult RenderPdfTexts([FromBody] Dictionary<string, object> jsonObject)
    {
        var viewer = CreateRenderer();
        var result = viewer.GetDocumentText(ConvertJson(jsonObject));
        return Content(JsonConvert.SerializeObject(result), "application/json");
    }

    [HttpPost("RenderThumbnailImages")]
    public IActionResult RenderThumbnails([FromBody] Dictionary<string, object> jsonObject)
    {
        var viewer = CreateRenderer();
        var result = viewer.GetThumbnailImages(ConvertJson(jsonObject));
        return Content(JsonConvert.SerializeObject(result), "application/json");
    }
    [HttpPost("Bookmarks")]
    public IActionResult Bookmarks([FromBody] Dictionary<string, object> jsonObject)
    {
        var viewer = CreateRenderer();
        var result = viewer.GetBookmarks(ConvertJson(jsonObject));
        return Content(JsonConvert.SerializeObject(result), "application/json");
    }

    [HttpPost("RenderAnnotationComments")]
    public IActionResult RenderAnnotations([FromBody] Dictionary<string, object> jsonObject)
    {
        var viewer = CreateRenderer();
        var result = viewer.GetAnnotationComments(ConvertJson(jsonObject));
        return Content(JsonConvert.SerializeObject(result), "application/json");
    }

    [HttpPost("PrintImages")]
    public IActionResult Print([FromBody] Dictionary<string, object> jsonObject)
    {
        var viewer = CreateRenderer();
        var result = viewer.GetPrintImage(ConvertJson(jsonObject));
        return Content(JsonConvert.SerializeObject(result), "application/json");
    }

    [HttpPost("Download")]
    public IActionResult Download([FromBody] Dictionary<string, object> jsonObject)
    {
        var viewer = CreateRenderer();
        var base64 = viewer.GetDocumentAsBase64(ConvertJson(jsonObject));
        return Content(base64, "text/plain");
    }

    [HttpPost("Unload")]
    public IActionResult Unload([FromBody] Dictionary<string, object> jsonObject)
    {
        var viewer = CreateRenderer();
        viewer.ClearCache(ConvertJson(jsonObject));
        return Ok();
    }

    private static Dictionary<string, string> ConvertJson(Dictionary<string, object> dict)
    {
        var output = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var kv in dict)
        {
            output[kv.Key] = kv.Value?.ToString() ?? string.Empty;
        }

        return output;
    }
}
