using System;
using ECM.File.Domain.Shares;

namespace ECM.File.Application.Shares;

internal static class ShareLinkMapper
{
    public static ShareLinkDto ToDto(ShareLink shareLink, ShareLinkOptions options, string? fallbackBaseUrl = null)
    {
        var (url, shortUrl) = BuildShareUrls(options.PublicBaseUrl, fallbackBaseUrl, shareLink.Code);

        return new ShareLinkDto(
            shareLink.Id,
            shareLink.Code,
            url,
            shortUrl,
            shareLink.OwnerUserId,
            shareLink.DocumentId,
            shareLink.VersionId,
            shareLink.SubjectType,
            shareLink.SubjectId,
            shareLink.Permissions,
            shareLink.ValidFrom,
            shareLink.ValidTo,
            shareLink.MaxViews,
            shareLink.MaxDownloads,
            shareLink.FileName,
            shareLink.FileExtension,
            shareLink.FileContentType,
            shareLink.FileSizeBytes,
            shareLink.FileCreatedAt,
            shareLink.GetStatus(DateTimeOffset.UtcNow),
            shareLink.RequiresPassword,
            shareLink.CreatedAt,
            shareLink.RevokedAt);
    }

    private static (string Url, string ShortUrl) BuildShareUrls(string? configuredBaseUrl, string? fallbackBaseUrl, string code)
    {
        var encodedCode = Uri.EscapeDataString(code);

        var effectiveBaseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? fallbackBaseUrl
            : configuredBaseUrl;

        if (string.IsNullOrWhiteSpace(effectiveBaseUrl))
        {
            return ($"/s/?code={encodedCode}", $"/s/{encodedCode}");
        }

        var trimmed = effectiveBaseUrl.TrimEnd('/');
        return ($"{trimmed}/s/?code={encodedCode}", $"{trimmed}/s/{encodedCode}");
    }
}
