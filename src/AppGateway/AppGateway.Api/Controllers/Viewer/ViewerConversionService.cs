using System.Text;

using AppGateway.Infrastructure.Ecm;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Syncfusion.DocIO.DLS;
using Syncfusion.EJ2.Spreadsheet;
using Syncfusion.EJ2.WordEditor;

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

    private DocumentFileContent? ConvertWordToSfdt(DocumentFileContent source)
    {
        try
        {
            using var input = new MemoryStream(source.Content);
            var format = ResolveWordFormat(source);
            using var document = WordDocument.Load(input, format);

            using var output = new MemoryStream();
            document.Save(output, FormatType.Sfdt);

            var fileName = Path.ChangeExtension(source.FileName ?? "document", ".sfdt");
            return new DocumentFileContent(
                output.ToArray(),
                "application/json",
                fileName,
                source.LastModifiedUtc,
                enableRangeProcessing: false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to convert Word document to SFDT");
            return null;
        }
    }

    private DocumentFileContent? ConvertExcelToJson(DocumentFileContent source)
    {
        try
        {
            var base64 = Convert.ToBase64String(source.Content);

            var request = new OpenRequest
            {
                File = new ImportRequest
                {
                    File = base64,
                    FileName = source.FileName ?? "workbook.xlsx",
                    Type = ResolveExcelType(source),
                },
            };

            var workbookJson = Workbook.Open(request);
            var fileName = Path.ChangeExtension(source.FileName ?? "workbook", ".json");

            return new DocumentFileContent(
                Encoding.UTF8.GetBytes(workbookJson),
                "application/json",
                fileName,
                source.LastModifiedUtc,
                enableRangeProcessing: false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to convert Excel document to Spreadsheet JSON");
            return null;
        }
    }

    private static FormatType ResolveWordFormat(DocumentFileContent source)
    {
        var extension = Path.GetExtension(source.FileName)?.TrimStart('.').ToLowerInvariant();

        return extension switch
        {
            "doc" => FormatType.Doc,
            "dot" => FormatType.Dot,
            "rtf" => FormatType.Rtf,
            _ => FormatType.Docx,
        };
    }

    private static string ResolveExcelType(DocumentFileContent source)
    {
        var contentType = source.ContentType?.ToLowerInvariant();
        if (!string.IsNullOrEmpty(contentType))
        {
            if (contentType.Contains("csv"))
            {
                return "csv";
            }

            if (contentType.Contains("spreadsheetml"))
            {
                return "xlsx";
            }
        }

        var extension = Path.GetExtension(source.FileName)?.TrimStart('.').ToLowerInvariant();
        return extension switch
        {
            "csv" => "csv",
            "xls" => "xls",
            _ => "xlsx",
        };
    }
}
