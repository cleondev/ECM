using AppGateway.Api.Auth;
using AppGateway.Api.Controllers.Documents;
using AppGateway.Contracts.Documents;
using AppGateway.Infrastructure.Ecm;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AppGateway.Api.Controllers.Viewer;

[ApiController]
[Route("api/viewer")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class ViewerController(
    IEcmApiClient ecmApiClient,
    ViewerConversionService conversionService,
    ILogger<ViewerController> logger) : ControllerBase
{
    private readonly IEcmApiClient _ecmApiClient = ecmApiClient;
    private readonly ViewerConversionService _conversionService = conversionService;
    private readonly ILogger<ViewerController> _logger = logger;

    [HttpGet("{versionId:guid}")]
    [ProducesResponseType(typeof(ViewerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var versionResult = await _ecmApiClient.GetDocumentVersionAsync(versionId, cancellationToken);
        if (versionResult.IsForbidden)
        {
            _logger.LogWarning("Forbidden access to version {VersionId}", versionId);
            return Forbid(authenticationSchemes: [GatewayAuthenticationSchemes.Default]);
        }

        var version = versionResult.Payload;
        if (version is null)
        {
            return NotFound();
        }

        var viewerType = ViewerTypeMapper.Resolve(version.MimeType, version.StorageKey);
        var response = CreateViewerResponse(version, viewerType);
        return Ok(response);
    }

    [HttpGet("word/{versionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWordViewerAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var preview = await _ecmApiClient.GetDocumentVersionPreviewAsync(versionId, cancellationToken);
        if (preview.IsForbidden)
        {
            return Forbid(authenticationSchemes: [GatewayAuthenticationSchemes.Default]);
        }

        if (preview.Payload is null)
        {
            return NotFound();
        }

        var sfdt = await _conversionService.ConvertWordToSfdtAsync(versionId, preview.Payload);
        return sfdt is null ? Problem("Không thể chuyển đổi tài liệu Word." ) : DocumentFileResultFactory.Create(sfdt);
    }

    [HttpGet("excel/{versionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExcelViewerAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var preview = await _ecmApiClient.GetDocumentVersionPreviewAsync(versionId, cancellationToken);
        if (preview.IsForbidden)
        {
            return Forbid(authenticationSchemes: [GatewayAuthenticationSchemes.Default]);
        }

        if (preview.Payload is null)
        {
            return NotFound();
        }

        var workbookJson = await _conversionService.ConvertExcelToJsonAsync(versionId, preview.Payload);
        return workbookJson is null
            ? Problem("Không thể chuyển đổi bảng tính.")
            : DocumentFileResultFactory.Create(workbookJson);
    }

    private ViewerResponse CreateViewerResponse(DocumentVersionDto version, string viewerType)
    {
        var basePath = HttpContext?.Request.PathBase ?? PathString.Empty;
        string BuildUrl(string relativePath) => $"{basePath}{relativePath}";

        var previewUrl = BuildUrl($"/api/documents/files/preview/{version.Id}");
        var downloadUrl = BuildUrl($"/api/documents/files/download/{version.Id}");
        var thumbnailUrl = BuildUrl($"/api/documents/files/thumbnails/{version.Id}?w=400&h=400&fit=contain");

        var serviceUrl = viewerType == ViewerTypes.Pdf ? BuildUrl($"/api/viewer/pdf/{version.Id}") : null;
        var wordUrl = viewerType == ViewerTypes.Word ? BuildUrl($"/api/viewer/word/{version.Id}") : null;
        var excelUrl = viewerType == ViewerTypes.Excel ? BuildUrl($"/api/viewer/excel/{version.Id}") : null;

        return new ViewerResponse(
            version,
            viewerType,
            previewUrl,
            downloadUrl,
            thumbnailUrl,
            serviceUrl is not null ? new ViewerView(serviceUrl) : null,
            wordUrl,
            excelUrl,
            serviceUrl);
    }
}
