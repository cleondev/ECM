using System.Net;
using System.Security.Claims;
using System.Linq;
using ECM.Abstractions.Files;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents.Queries;
using ECM.Document.Domain.Shares;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECM.Document.Application.Shares;

public sealed class ShareAccessService(
    IShareLinkRepository repository,
    ISharePasswordHasher passwordHasher,
    IDocumentVersionReadService documentVersions,
    IFileAccessGateway fileAccess,
    ISystemClock clock,
    IOptions<ShareLinkOptions> options,
    ILogger<ShareAccessService> logger)
{
    private readonly IShareLinkRepository _repository = repository;
    private readonly ISharePasswordHasher _passwordHasher = passwordHasher;
    private readonly IDocumentVersionReadService _documentVersions = documentVersions;
    private readonly IFileAccessGateway _fileAccess = fileAccess;
    private readonly ISystemClock _clock = clock;
    private readonly ShareLinkOptions _options = options.Value;
    private readonly ILogger<ShareAccessService> _logger = logger;

    public async Task<OperationResult<ShareInterstitialResponse>> GetInterstitialAsync(
        string code,
        ClaimsPrincipal? user,
        string? password,
        IPAddress? remoteIp,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var evaluationResult = await EvaluateAsync(
            code,
            user,
            password,
            remoteIp,
            userAgent,
            requirePassword: false,
            checkDownloadQuota: false,
            cancellationToken);

        if (evaluationResult.IsFailure || evaluationResult.Value is null)
        {
            return OperationResult<ShareInterstitialResponse>.Failure([.. evaluationResult.Errors]);
        }

        var evaluation = evaluationResult.Value;
        var share = evaluation.Share;
        var viewsUsed = evaluation.ViewsUsed;
        var downloadsUsed = evaluation.DownloadsUsed;

        if (share.RequiresPassword && !evaluation.PasswordValidated)
        {
            var pendingResponse = BuildResponse(
                share,
                passwordValid: false,
                canDownload: false,
                viewsUsed,
                downloadsUsed);

            return OperationResult<ShareInterstitialResponse>.Success(pendingResponse);
        }

        await LogAccessEventAsync(share.Id, "view", true, remoteIp, userAgent, cancellationToken);

        var response = BuildResponse(
            share,
            passwordValid: true,
            canDownload: share.HasPermission(SharePermission.Download),
            viewsUsed + 1,
            downloadsUsed);

        _logger.LogInformation(
            "Recorded view for share {ShareId} (code {Code}).",
            share.Id,
            share.Code);

        return OperationResult<ShareInterstitialResponse>.Success(response);
    }

    public async Task<OperationResult<bool>> VerifyPasswordAsync(
        string code,
        string password,
        ClaimsPrincipal? user,
        IPAddress? remoteIp,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return OperationResult<bool>.Failure("PasswordRequired");
        }

        var evaluationResult = await EvaluateAsync(
            code,
            user,
            password,
            remoteIp,
            userAgent,
            requirePassword: true,
            checkDownloadQuota: false,
            cancellationToken);

        if (evaluationResult.IsFailure || evaluationResult.Value is null)
        {
            return OperationResult<bool>.Failure([.. evaluationResult.Errors]);
        }

        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<FileDownloadLink>> CreateDownloadLinkAsync(
        string code,
        ClaimsPrincipal? user,
        string? password,
        IPAddress? remoteIp,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var evaluationResult = await EvaluateAsync(
            code,
            user,
            password,
            remoteIp,
            userAgent,
            requirePassword: true,
            checkDownloadQuota: true,
            cancellationToken);

        if (evaluationResult.IsFailure || evaluationResult.Value is null)
        {
            return OperationResult<FileDownloadLink>.Failure([.. evaluationResult.Errors]);
        }

        var evaluation = evaluationResult.Value;
        var share = evaluation.Share;

        if (!share.HasPermission(SharePermission.Download))
        {
            return OperationResult<FileDownloadLink>.Failure("DownloadNotAllowed");
        }

        if (!share.VersionId.HasValue)
        {
            return OperationResult<FileDownloadLink>.Failure("VersionRequired");
        }

        var version = await _documentVersions.GetByIdAsync(share.VersionId.Value, cancellationToken);
        if (version is null)
        {
            return OperationResult<FileDownloadLink>.Failure("DocumentVersionNotFound");
        }

        var lifetime = _options.DefaultPresignLifetime;
        var linkResult = await _fileAccess.GetDownloadLinkAsync(
            version.StorageKey,
            lifetime,
            share.FileName,
            cancellationToken);
        if (linkResult.IsFailure || linkResult.Value is null)
        {
            return OperationResult<FileDownloadLink>.Failure([.. linkResult.Errors]);
        }

        await LogAccessEventAsync(share.Id, "download", true, remoteIp, userAgent, cancellationToken);

        _logger.LogInformation(
            "Issued presigned download link for share {ShareId} (code {Code}).",
            share.Id,
            share.Code);

        return OperationResult<FileDownloadLink>.Success(linkResult.Value);
    }

    private ShareInterstitialResponse BuildResponse(
        ShareLink share,
        bool passwordValid,
        bool canDownload,
        long viewsUsed,
        long downloadsUsed)
    {
        var file = new ShareFileDescriptor(
            share.FileName,
            share.FileExtension,
            share.FileContentType,
            share.FileSizeBytes,
            share.FileCreatedAt);

        var quota = new ShareQuotaSnapshot(
            share.MaxViews,
            share.MaxDownloads,
            viewsUsed,
            downloadsUsed);

        return new ShareInterstitialResponse(
            share.Id,
            share.Code,
            share.SubjectType,
            share.GetStatus(_clock.UtcNow),
            share.RequiresPassword,
            passwordValid,
            canDownload,
            file,
            quota);
    }

    private async Task<OperationResult<ShareEvaluation>> EvaluateAsync(
        string code,
        ClaimsPrincipal? user,
        string? password,
        IPAddress? remoteIp,
        string? userAgent,
        bool requirePassword,
        bool checkDownloadQuota,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return OperationResult<ShareEvaluation>.Failure("CodeRequired");
        }

        var share = await _repository.GetByCodeAsync(code.Trim(), cancellationToken);
        if (share is null)
        {
            return OperationResult<ShareEvaluation>.Failure("ShareNotFound");
        }

        var now = _clock.UtcNow;

        if (share.RevokedAt.HasValue)
        {
            return OperationResult<ShareEvaluation>.Failure("ShareRevoked");
        }

        if (now < share.ValidFrom)
        {
            return OperationResult<ShareEvaluation>.Failure("ShareNotYetValid");
        }

        if (share.ValidTo.HasValue && now > share.ValidTo.Value)
        {
            return OperationResult<ShareEvaluation>.Failure("ShareExpired");
        }

        if (!IsAuthorized(share, user))
        {
            await LogAccessEventAsync(share.Id, "view", false, remoteIp, userAgent, cancellationToken);
            return OperationResult<ShareEvaluation>.Failure("ShareNotAuthorized");
        }

        if (share.AllowedIps.Count > 0)
        {
            if (remoteIp is null || !share.AllowedIps.Any(ip => ip.Equals(remoteIp)))
            {
                await LogAccessEventAsync(share.Id, "view", false, remoteIp, userAgent, cancellationToken);
                return OperationResult<ShareEvaluation>.Failure("ShareIpNotAllowed");
            }
        }

        var viewsUsed = await _repository.CountSuccessfulViewsAsync(share.Id, cancellationToken);
        if (share.MaxViews is not null && viewsUsed >= share.MaxViews)
        {
            return OperationResult<ShareEvaluation>.Failure("ShareViewQuotaExceeded");
        }

        var downloadsUsed = await _repository.CountSuccessfulDownloadsAsync(share.Id, cancellationToken);
        if (checkDownloadQuota && share.MaxDownloads is not null && downloadsUsed >= share.MaxDownloads)
        {
            return OperationResult<ShareEvaluation>.Failure("ShareDownloadQuotaExceeded");
        }

        var passwordValidated = !share.RequiresPassword;

        if (share.RequiresPassword)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                if (requirePassword)
                {
                    return OperationResult<ShareEvaluation>.Failure("PasswordRequired");
                }

                return OperationResult<ShareEvaluation>.Success(new ShareEvaluation(
                    share,
                    PasswordValidated: false,
                    viewsUsed,
                    downloadsUsed));
            }

            if (!_passwordHasher.Verify(password, share.PasswordHash!))
            {
                await LogAccessEventAsync(share.Id, "password_failed", false, remoteIp, userAgent, cancellationToken);
                return OperationResult<ShareEvaluation>.Failure("PasswordInvalid");
            }

            passwordValidated = true;
        }

        return OperationResult<ShareEvaluation>.Success(new ShareEvaluation(
            share,
            passwordValidated,
            viewsUsed,
            downloadsUsed));
    }

    private async Task LogAccessEventAsync(
        Guid shareId,
        string action,
        bool succeeded,
        IPAddress? remoteIp,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var evt = new ShareAccessEvent(
            shareId,
            _clock.UtcNow,
            action,
            succeeded,
            remoteIp,
            userAgent);

        await _repository.AddAccessEventAsync(evt, cancellationToken);
    }

    private static bool IsAuthorized(ShareLink share, ClaimsPrincipal? user)
    {
        return share.SubjectType switch
        {
            ShareSubjectType.Public => true,
            ShareSubjectType.User => UserMatches(share.SubjectId, user),
            ShareSubjectType.Group => GroupMatches(share.SubjectId, user),
            _ => false,
        };
    }

    private static bool UserMatches(Guid? subjectId, ClaimsPrincipal? user)
    {
        if (!subjectId.HasValue || user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var userId = GetGuidClaim(user, ClaimTypes.NameIdentifier)
            ?? GetGuidClaim(user, "sub");

        return userId.HasValue && userId == subjectId;
    }

    private static bool GroupMatches(Guid? subjectId, ClaimsPrincipal? user)
    {
        if (!subjectId.HasValue || user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var claims = user.Claims
            .Where(claim => claim.Type is ClaimTypes.GroupSid or "group" or "groups")
            .Select(claim => TryParseGuid(claim.Value))
            .Where(id => id.HasValue)
            .Select(id => id!.Value);

        return claims.Contains(subjectId.Value);
    }

    private static Guid? GetGuidClaim(ClaimsPrincipal user, string claimType)
    {
        var value = user.FindFirstValue(claimType);
        return TryParseGuid(value);
    }

    private static Guid? TryParseGuid(string? value)
    {
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private sealed record ShareEvaluation(
        ShareLink Share,
        bool PasswordValidated,
        long ViewsUsed,
        long DownloadsUsed);
}
