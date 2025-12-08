using AppGateway.Contracts.Documents;

namespace AppGateway.Api.Controllers.Viewer;

public sealed record ViewerResponse(
    DocumentVersionDto Version,
    string ViewerType,
    string PreviewUrl,
    string DownloadUrl,
    string ThumbnailUrl,
    string? WordViewerUrl,
    string? ExcelViewerUrl);
