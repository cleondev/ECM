using ECM.File.Domain.Shares;

namespace ECM.File.Application.Shares;

public sealed record ShareFileDescriptor(
    string Name,
    string? Extension,
    string ContentType,
    long SizeBytes,
    DateTimeOffset? CreatedAtUtc);

public sealed record ShareQuotaSnapshot(
    int? MaxViews,
    int? MaxDownloads,
    long ViewsUsed,
    long DownloadsUsed);

public sealed record ShareInterstitialResponse(
    Guid ShareId,
    string Code,
    ShareSubjectType SubjectType,
    ShareLinkStatus Status,
    bool RequiresPassword,
    bool PasswordValid,
    bool CanDownload,
    ShareFileDescriptor File,
    ShareQuotaSnapshot Quota);
