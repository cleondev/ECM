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
    private readonly HashSet<string> _namespaces = new(StringComparer.OrdinalIgnoreCase);

    public FakeTagLabelRepository(IEnumerable<string>? namespaces = null)
    {
        if (namespaces is null)
        {
            return;
        }

        foreach (var name in namespaces)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                _namespaces.Add(name);
            }
        }
    }

    public CancellationToken? CapturedToken { get; private set; }

    public IReadOnlyCollection<TagLabel> StoredTags => _tags.Values.ToArray();

    public void Seed(TagLabel tagLabel)
    {
        ArgumentNullException.ThrowIfNull(tagLabel);
        _tags[tagLabel.Id] = tagLabel;
        _namespaces.Add(tagLabel.NamespaceSlug);
    }

    public Task<TagLabel?> GetByIdAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        _tags.TryGetValue(tagId, out var tagLabel);
        return Task.FromResult(tagLabel);
    }

    public Task<TagLabel?> GetByNamespaceAndPathAsync(
        string namespaceSlug,
        string path,
        CancellationToken cancellationToken = default)
    {
        var match = _tags.Values.FirstOrDefault(tag =>
            string.Equals(tag.NamespaceSlug, namespaceSlug, StringComparison.OrdinalIgnoreCase)
            && string.Equals(tag.Path, path, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match);
    }

    public Task<bool> NamespaceExistsAsync(string namespaceSlug, CancellationToken cancellationToken = default)
        => Task.FromResult(_namespaces.Contains(namespaceSlug));

    public Task<TagLabel> AddAsync(TagLabel tagLabel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tagLabel);
        _tags[tagLabel.Id] = tagLabel;
        _namespaces.Add(tagLabel.NamespaceSlug);
        CapturedToken = cancellationToken;
        return Task.FromResult(tagLabel);
    }

    public Task<TagLabel> UpdateAsync(TagLabel tagLabel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tagLabel);
        _tags[tagLabel.Id] = tagLabel;
        _namespaces.Add(tagLabel.NamespaceSlug);
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
