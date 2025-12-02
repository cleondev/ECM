using ECM.Document.Domain.Shares;

namespace ECM.Document.Application.Shares;

public sealed record ShareLinkDto(
    Guid Id,
    string Code,
    string Url,
    string ShortUrl,
    Guid OwnerUserId,
    Guid DocumentId,
    Guid? VersionId,
    ShareSubjectType SubjectType,
    Guid? SubjectId,
    SharePermission Permissions,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    int? MaxViews,
    int? MaxDownloads,
    string FileName,
    string? FileExtension,
    string FileContentType,
    long FileSizeBytes,
    DateTimeOffset? FileCreatedAt,
    ShareLinkStatus Status,
    bool RequiresPassword,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RevokedAt);
