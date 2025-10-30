using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.Document.Domain.Tags;

namespace ECM.Document.Application.Tags.Repositories;

public interface ITagLabelRepository
{
    Task<TagLabel?> GetByIdAsync(Guid tagId, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithNameAsync(
        Guid namespaceId,
        Guid? parentId,
        string name,
        Guid? excludeTagId,
        CancellationToken cancellationToken = default);

    Task<bool> HasChildrenAsync(Guid tagId, CancellationToken cancellationToken = default);

    Task<TagLabel> AddAsync(TagLabel tagLabel, CancellationToken cancellationToken = default);

    Task<TagLabel> UpdateAsync(TagLabel tagLabel, CancellationToken cancellationToken = default);

    Task RemoveAsync(TagLabel tagLabel, CancellationToken cancellationToken = default);
}
