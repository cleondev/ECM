using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.Document.Domain.Tags;

namespace ECM.Document.Application.Tags.Repositories;

public interface ITagNamespaceRepository
{
    Task<TagNamespace?> GetAsync(Guid namespaceId, CancellationToken cancellationToken = default);
}
