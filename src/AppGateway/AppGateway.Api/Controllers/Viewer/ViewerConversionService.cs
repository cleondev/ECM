using System.Text;

using AppGateway.Infrastructure.Ecm;

using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json;

using Syncfusion.EJ2.DocumentEditor;
using Syncfusion.EJ2.Spreadsheet;

namespace AppGateway.Api.Controllers.Viewer;

public sealed class ViewerConversionService(IMemoryCache cache, ILogger<ViewerConversionService> logger)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(20);

    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<ViewerConversionService> _logger = logger;

    public Task<DocumentFileContent?> ConvertWordToSfdtAsync(Guid versionId, DocumentFileContent source)
    {
        return _cache.GetOrCreateAsync(
            $"viewer:word:{versionId}",
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return Task.FromResult(ConvertWordToSfdt(source));
            });
    }

    public Task<DocumentFileContent?> ConvertExcelToJsonAsync(Guid versionId, DocumentFileContent source)
    {
        return _cache.GetOrCreateAsync(
            $"viewer:excel:{versionId}",
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return Task.FromResult(ConvertExcelToJson(source));
            });
    }

    /// <summary>
    /// Convert Word → SFDT JSON (DocumentEditor EJ2)
    /// </summary>
    private DocumentFileContent? ConvertWordToSfdt(DocumentFileContent source)
    {
        try
        {
            var stream = new MemoryStream(source.Content);
            var format = ResolveWordFormat(source);

            // EJ2 WordDocument: KHÔNG IDisposable → không dùng using
            var document = WordDocument.Load(stream, format);

            var sfdt = JsonConvert.SerializeObject(document);
            var fileName = Path.ChangeExtension(source.FileName ?? "document", ".sfdt");

            return new DocumentFileContent(
                Encoding.UTF8.GetBytes(sfdt),
                "application/json",
                fileName,
                source.LastModifiedUtc,
                EnableRangeProcessing: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert Word to SFDT.");
            return null;
        }
    }

    /// <summary>
    /// Convert Excel → JSON (Spreadsheet EJ2)
    /// </summary>
    private DocumentFileContent? ConvertExcelToJson(DocumentFileContent source)
    {
        try
        {
            var stream = new MemoryStream(source.Content);

            // Giả lập IFormFile → dùng lại API Workbook.Open(OpenRequest)
            IFormFile formFile = new FormFile(
                baseStream: stream,
                baseStreamOffset: 0,
                length: stream.Length,
                name: "file",
                fileName: source.FileName ?? "workbook.xlsx");

            var request = new OpenRequest
            {
                File = formFile
            };

            var json = Workbook.Open(request);
            var fileName = Path.ChangeExtension(source.FileName ?? "workbook", ".json");

            return new DocumentFileContent(
                Encoding.UTF8.GetBytes(json),
                "application/json",
                fileName,
                source.LastModifiedUtc,
                EnableRangeProcessing: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert Excel to JSON.");
            return null;
        }
    }

    /// <summary>
    /// EJ2 FormatType (không có DOT)
    /// </summary>
    private static FormatType ResolveWordFormat(DocumentFileContent source)
    {
        var ext = Path.GetExtension(source.FileName)?.TrimStart('.').ToLowerInvariant();

        return ext switch
        {
            "doc" => FormatType.Doc,
            "rtf" => FormatType.Rtf,
            "txt" => FormatType.Txt,
            _ => FormatType.Docx,
        };
    }
}
