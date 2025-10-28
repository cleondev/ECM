using ECM.File.Domain.Shares;

namespace ECM.File.Application.Shares;

public interface IShareLinkRepository
{
    Task AddAsync(ShareLink shareLink, CancellationToken cancellationToken = default);

    Task<ShareLink?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ShareLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    Task UpdateAsync(ShareLink shareLink, CancellationToken cancellationToken = default);

    Task<ShareStatistics?> GetStatisticsAsync(Guid shareId, CancellationToken cancellationToken = default);

    Task<long> CountSuccessfulViewsAsync(Guid shareId, CancellationToken cancellationToken = default);

    Task<long> CountSuccessfulDownloadsAsync(Guid shareId, CancellationToken cancellationToken = default);

    Task AddAccessEventAsync(ShareAccessEvent accessEvent, CancellationToken cancellationToken = default);
}
