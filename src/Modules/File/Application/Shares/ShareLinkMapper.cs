using ECM.File.Domain.Shares;

namespace ECM.File.Application.Shares;

internal static class ShareLinkMapper
{
    public static ShareLinkDto ToDto(ShareLink shareLink, ShareLinkOptions options)
    {
        var url = BuildShareUrl(options.PublicBaseUrl, shareLink.Code);

        return new ShareLinkDto(
            shareLink.Id,
            shareLink.Code,
            url,
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

    private static string BuildShareUrl(string? baseUrl, string code)
    {
        var encodedCode = Uri.EscapeDataString(code);

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return $"/s/?code={encodedCode}";
        }

        var trimmed = baseUrl.TrimEnd('/');
        return $"{trimmed}/s/?code={encodedCode}";
    }
}
