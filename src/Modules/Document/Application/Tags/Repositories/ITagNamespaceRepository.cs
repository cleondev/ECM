using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.Document.Domain.Tags;

namespace ECM.Document.Application.Tags.Repositories;

public interface ITagNamespaceRepository
{
    Task<TagNamespace?> GetAsync(Guid namespaceId, CancellationToken cancellationToken = default);

    Task<TagNamespace?> GetUserNamespaceAsync(Guid ownerUserId, CancellationToken cancellationToken = default);

    Task<TagNamespace> AddAsync(TagNamespace tagNamespace, CancellationToken cancellationToken = default);
}
