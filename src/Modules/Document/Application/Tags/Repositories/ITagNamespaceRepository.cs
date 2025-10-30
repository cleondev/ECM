using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.Document.Domain.Tags;

namespace ECM.Document.Application.Tags.Repositories;

public interface ITagNamespaceRepository
{
    Task<bool> ExistsAsync(string namespaceSlug, CancellationToken cancellationToken = default);

    Task EnsureUserNamespaceAsync(
        string namespaceSlug,
        Guid? ownerUserId,
        string? displayName,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken = default);
}
