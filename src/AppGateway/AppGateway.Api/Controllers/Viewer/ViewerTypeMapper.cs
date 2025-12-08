using System.Collections.Generic;
using System.IO;

namespace AppGateway.Api.Controllers.Viewer;

internal static class ViewerTypeMapper
{
    private static readonly HashSet<string> WordMimeTypes =
    [
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/rtf",
    ];

    private static readonly HashSet<string> ExcelMimeTypes =
    [
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/csv",
    ];

    private static readonly HashSet<string> WordExtensions =
    [
        "doc",
        "docx",
        "dot",
        "dotx",
        "rtf",
    ];

    private static readonly HashSet<string> ExcelExtensions =
    [
        "xls",
        "xlsx",
        "xlsm",
        "xltx",
        "csv",
    ];

    public static string Resolve(string? mimeType, string? storageKey)
    {
        var normalizedMime = Normalize(mimeType);
        if (!string.IsNullOrEmpty(normalizedMime))
        {
            if (normalizedMime.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                return ViewerTypes.Video;
            }

            if (normalizedMime.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return ViewerTypes.Image;
            }

            if (string.Equals(normalizedMime, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                return ViewerTypes.Pdf;
            }

            if (WordMimeTypes.Contains(normalizedMime))
            {
                return ViewerTypes.Word;
            }

            if (ExcelMimeTypes.Contains(normalizedMime))
            {
                return ViewerTypes.Excel;
            }
        }

        var extension = GetExtension(storageKey);
        if (extension is not null)
        {
            if (WordExtensions.Contains(extension))
            {
                return ViewerTypes.Word;
            }

            if (ExcelExtensions.Contains(extension))
            {
                return ViewerTypes.Excel;
            }

            if (extension is "pdf")
            {
                return ViewerTypes.Pdf;
            }

            if (extension is "mp4" or "mov" or "avi")
            {
                return ViewerTypes.Video;
            }

            if (extension is "png" or "jpg" or "jpeg" or "gif" or "webp" or "bmp")
            {
                return ViewerTypes.Image;
            }
        }

        return ViewerTypes.Unsupported;
    }

    private static string? GetExtension(string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return null;
        }

        var extension = Path.GetExtension(storageKey);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        return extension.TrimStart('.').ToLowerInvariant();
    }

    private static string? Normalize(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            return null;
        }

        return mimeType.Trim();
    }
}
