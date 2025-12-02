using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Domain.Shares;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECM.Document.Application.Shares;

public sealed class ShareLinkService(
    IShareLinkRepository repository,
    ISystemClock clock,
    IOptions<ShareLinkOptions> options,
    ILogger<ShareLinkService> logger)
{
    private readonly IShareLinkRepository _repository = repository;
    private readonly ISystemClock _clock = clock;
    private readonly ShareLinkOptions _options = options.Value;
    private readonly ILogger<ShareLinkService> _logger = logger;

    public async Task<ShareLinkDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var share = await _repository.GetByIdAsync(id, cancellationToken);
        return share is null ? null : ShareLinkMapper.ToDto(share, _options);
    }

    public async Task<ShareLinkDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var share = await _repository.GetByCodeAsync(code.Trim(), cancellationToken);
        return share is null ? null : ShareLinkMapper.ToDto(share, _options);
    }

    public async Task<OperationResult> RevokeAsync(Guid shareId, CancellationToken cancellationToken = default)
    {
        if (shareId == Guid.Empty)
        {
            return OperationResult.Failure("ShareIdRequired");
        }

        var share = await _repository.GetByIdAsync(shareId, cancellationToken);
        if (share is null)
        {
            return OperationResult.Failure("ShareNotFound");
        }

        if (share.RevokedAt.HasValue)
        {
            return OperationResult.Success();
        }

        var now = _clock.UtcNow;
        share.Revoke(now);
        await _repository.UpdateAsync(share, cancellationToken);

        _logger.LogInformation("Share link {ShareId} revoked at {Timestamp}.", share.Id, now);
        return OperationResult.Success();
    }

    public async Task<ShareStatisticsDto?> GetStatisticsAsync(Guid shareId, CancellationToken cancellationToken = default)
    {
        if (shareId == Guid.Empty)
        {
            return null;
        }

        var stats = await _repository.GetStatisticsAsync(shareId, cancellationToken);
        if (stats is null)
        {
            return null;
        }

        return new ShareStatisticsDto(stats.ShareId, stats.Views, stats.Downloads, stats.Failures, stats.LastAccessUtc);
    }
}
