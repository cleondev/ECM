namespace ECM.Document.Domain.Tags;

public interface ITagLabelRepository
{
    Task<TagLabel?> GetByIdAsync(Guid tagId, CancellationToken cancellationToken = default);

    Task<TagLabel?> GetByNamespaceAndPathAsync(string namespaceSlug, string path, CancellationToken cancellationToken = default);

    Task<bool> NamespaceExistsAsync(string namespaceSlug, CancellationToken cancellationToken = default);

    Task<TagLabel> AddAsync(TagLabel tagLabel, CancellationToken cancellationToken = default);

    Task RemoveAsync(TagLabel tagLabel, CancellationToken cancellationToken = default);
}
