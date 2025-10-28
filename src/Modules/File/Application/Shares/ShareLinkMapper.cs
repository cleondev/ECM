using ECM.File.Domain.Shares;

namespace ECM.File.Application.Shares;

internal static class ShareLinkMapper
{
    public static ShareLinkDto ToDto(ShareLink shareLink, ShareLinkOptions options)
    {
        var baseUrl = options.PublicBaseUrl;
        var url = string.IsNullOrWhiteSpace(baseUrl)
            ? $"/s/{shareLink.Code}"
            : CombineUrl(baseUrl!, shareLink.Code);

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

    private static string CombineUrl(string baseUrl, string code)
    {
        var trimmed = baseUrl.TrimEnd('/');
        return $"{trimmed}/s/{code}";
    }
}
