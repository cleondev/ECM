using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.Document.Domain.Tags;

namespace ECM.Document.Application.Tags.Repositories;

public interface ITagLabelRepository
{
    Task<TagLabel?> GetByIdAsync(Guid tagId, CancellationToken cancellationToken = default);

    Task<TagLabel?> GetByNamespaceAndPathAsync(string namespaceSlug, string path, CancellationToken cancellationToken = default);

    Task<bool> NamespaceExistsAsync(string namespaceSlug, CancellationToken cancellationToken = default);

    Task<TagLabel> AddAsync(TagLabel tagLabel, CancellationToken cancellationToken = default);

    Task<TagLabel> UpdateAsync(TagLabel tagLabel, CancellationToken cancellationToken = default);

    Task RemoveAsync(TagLabel tagLabel, CancellationToken cancellationToken = default);
}
