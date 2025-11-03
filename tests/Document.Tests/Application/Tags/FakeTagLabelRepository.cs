using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.Document.Application.Tags.Repositories;
using ECM.Document.Domain.Tags;

namespace Document.Tests.Application.Tags;

internal sealed class FakeTagLabelRepository : ITagLabelRepository
{
    private readonly Dictionary<Guid, TagLabel> _tags = [];

    public CancellationToken? CapturedToken { get; private set; }

    public IReadOnlyCollection<TagLabel> StoredTags => [.. _tags.Values];

    public void Seed(TagLabel tagLabel)
    {
        ArgumentNullException.ThrowIfNull(tagLabel);
        _tags[tagLabel.Id] = tagLabel;
    }

    public Task<TagLabel?> GetByIdAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        _tags.TryGetValue(tagId, out var tagLabel);
        return Task.FromResult(tagLabel);
    }

    public Task<TagLabel[]> ListWithNamespaceAsync(
        Guid? ownerUserId,
        Guid? primaryGroupId,
        CancellationToken cancellationToken = default)
    {
        CapturedToken = cancellationToken;

        var ordered = _tags.Values
            .OrderBy(label => label.NamespaceId)
            .ThenBy(label => label.ParentId.HasValue ? 1 : 0)
            .ThenBy(label => label.SortOrder)
            .ThenBy(label => label.Name, StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult(ordered);
    }

    public Task<bool> ExistsWithNameAsync(
        Guid namespaceId,
        Guid? parentId,
        string name,
        Guid? excludeTagId,
        CancellationToken cancellationToken = default)
    {
        var exists = _tags.Values.Any(tag =>
            tag.NamespaceId == namespaceId
            && tag.ParentId == parentId
            && string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase)
            && (!excludeTagId.HasValue || tag.Id != excludeTagId.Value));

        return Task.FromResult(exists);
    }

    public Task<bool> HasChildrenAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        var hasChildren = _tags.Values.Any(tag => tag.ParentId == tagId);
        return Task.FromResult(hasChildren);
    }

    public Task<TagLabel> AddAsync(TagLabel tagLabel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tagLabel);
        _tags[tagLabel.Id] = tagLabel;
        CapturedToken = cancellationToken;
        return Task.FromResult(tagLabel);
    }

    public Task<TagLabel> UpdateAsync(TagLabel tagLabel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tagLabel);
        _tags[tagLabel.Id] = tagLabel;
        CapturedToken = cancellationToken;
        return Task.FromResult(tagLabel);
    }

    public Task RemoveAsync(TagLabel tagLabel, CancellationToken cancellationToken = default)
    {
        if (tagLabel is not null)
        {
            _tags.Remove(tagLabel.Id);
        }

        CapturedToken = cancellationToken;
        return Task.CompletedTask;
    }
}
