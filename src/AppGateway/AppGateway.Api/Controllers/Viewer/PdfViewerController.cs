using System.Text.Json;

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

    /// <summary>
    /// Load PDF từ ECM vào PdfRenderer.
    /// </summary>
    [HttpPost("load")]
    public async Task<IActionResult> LoadAsync(
        Guid versionId,
        [FromBody] JsonElement jsonObject,
        CancellationToken cancellationToken)
    {
        if (jsonObject.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
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

        try
        {
            await using var stream = new MemoryStream(preview.Payload.Content);

            // PdfRenderer cần IMemoryCache
            var pdfViewer = new PdfRenderer(_cache);

            // Load trả về object → phải serialize
            var result = pdfViewer.Load(stream, ConvertJsonObject(jsonObject));

            return Content(JsonConvert.SerializeObject(result), "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load PDF for version {VersionId}", versionId);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Render trang PDF.
    /// </summary>
    [HttpPost("render")]
    public IActionResult RenderPdfPages([FromBody] JsonElement jsonObject)
    {
        if (jsonObject.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return BadRequest();
        }

        var pdfViewer = new PdfRenderer(_cache);
        var result = pdfViewer.GetPage(ConvertJsonObject(jsonObject));
        return Content(JsonConvert.SerializeObject(result), "application/json");
    }

    /// <summary>
    /// Render text trong PDF.
    /// LƯU Ý: API đúng là GetDocumentText, không phải GetText.
    /// </summary>
    [HttpPost("render-text")]
    public IActionResult RenderPdfTexts([FromBody] JsonElement jsonObject)
    {
        if (jsonObject.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return BadRequest();
        }

        var pdfViewer = new PdfRenderer(_cache);
        var result = pdfViewer.GetDocumentText(ConvertJsonObject(jsonObject));
        return Content(JsonConvert.SerializeObject(result), "application/json");
    }

    /// <summary>
    /// Render thumbnail.
    /// </summary>
    [HttpPost("render-thumbnails")]
    public IActionResult RenderThumbnails([FromBody] JsonElement jsonObject)
    {
        if (jsonObject.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return BadRequest();
        }

        var pdfViewer = new PdfRenderer(_cache);
        var result = pdfViewer.GetThumbnailImages(ConvertJsonObject(jsonObject));
        return Content(JsonConvert.SerializeObject(result), "application/json");
    }

    /// <summary>
    /// Lấy bookmarks.
    /// </summary>
    [HttpPost("bookmarks")]
    public IActionResult Bookmarks([FromBody] JsonElement jsonObject)
    {
        if (jsonObject.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return BadRequest();
        }

        var pdfViewer = new PdfRenderer(_cache);
        var result = pdfViewer.GetBookmarks(ConvertJsonObject(jsonObject));
        return Content(JsonConvert.SerializeObject(result), "application/json");
    }

    /// <summary>
    /// Lấy annotation comments.
    /// </summary>
    [HttpPost("annotations")]
    public IActionResult RenderAnnotations([FromBody] JsonElement jsonObject)
    {
        if (jsonObject.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return BadRequest();
        }

        var pdfViewer = new PdfRenderer(_cache);
        var result = pdfViewer.GetAnnotationComments(ConvertJsonObject(jsonObject));
        return Content(JsonConvert.SerializeObject(result), "application/json");
    }

    /// <summary>
    /// In: PdfViewer dùng GetPrintImage, không có Print().
    /// </summary>
    [HttpPost("print")]
    public IActionResult Print([FromBody] JsonElement jsonObject)
    {
        if (jsonObject.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return BadRequest();
        }

        var pdfViewer = new PdfRenderer(_cache);
        var result = pdfViewer.GetPrintImage(ConvertJsonObject(jsonObject));
        return Content(JsonConvert.SerializeObject(result), "application/json");
    }

    /// <summary>
    /// Download: dùng GetDocumentAsBase64, không có GetDocument().
    /// FE sẽ decode base64 để tải file.
    /// </summary>
    [HttpPost("download")]
    public IActionResult Download([FromBody] JsonElement jsonObject)
    {
        if (jsonObject.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return BadRequest();
        }

        var pdfViewer = new PdfRenderer(_cache);
        var documentBase64 = pdfViewer.GetDocumentAsBase64(ConvertJsonObject(jsonObject));

        // Syncfusion sample trả luôn base64 string (content-type mặc định text/plain).
        // Nếu muốn rõ ràng:
        return Content(documentBase64, "text/plain");
    }

    /// <summary>
    /// Giải phóng cache trên server.
    /// </summary>
    [HttpPost("unload")]
    public IActionResult Unload([FromBody] JsonElement jsonObject)
    {
        if (jsonObject.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return BadRequest();
        }

        var pdfViewer = new PdfRenderer(_cache);
        pdfViewer.ClearCache(ConvertJsonObject(jsonObject));
        return Ok();
    }

    private static Dictionary<string, string> ConvertJsonObject(JsonElement jsonObject)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var property in jsonObject.EnumerateObject())
        {
            var value = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => property.Value.GetRawText(),
                JsonValueKind.True => bool.TrueString.ToLowerInvariant(),
                JsonValueKind.False => bool.FalseString.ToLowerInvariant(),
                JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
                _ => property.Value.GetRawText(),
            };

            result[property.Name] = value;
        }

        return result;
    }
}
