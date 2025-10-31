using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.Document.Application.Tags.Repositories;
using ECM.Document.Domain.Tags;

namespace Document.Tests.Application.Tags;

internal sealed class FakeTagNamespaceRepository : ITagNamespaceRepository
{
    private readonly Dictionary<Guid, TagNamespace> _namespaces = [];

    public FakeTagNamespaceRepository(IEnumerable<TagNamespace>? namespaces = null)
    {
        if (namespaces is null)
        {
            return;
        }

        foreach (var tagNamespace in namespaces)
        {
            Seed(tagNamespace);
        }
    }

    public IReadOnlyCollection<TagNamespace> StoredNamespaces => [.. _namespaces.Values];

    public void Seed(TagNamespace tagNamespace)
    {
        ArgumentNullException.ThrowIfNull(tagNamespace);
        _namespaces[tagNamespace.Id] = tagNamespace;
    }

    public Task<TagNamespace?> GetAsync(Guid namespaceId, CancellationToken cancellationToken = default)
    {
        _namespaces.TryGetValue(namespaceId, out var tagNamespace);
        return Task.FromResult(tagNamespace);
    }

    public Task<TagNamespace?> GetUserNamespaceAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var tagNamespace = _namespaces.Values.FirstOrDefault(ns => ns.Scope == "user" && ns.OwnerUserId == ownerUserId);
        return Task.FromResult(tagNamespace);
    }

    public Task<TagNamespace> AddAsync(TagNamespace tagNamespace, CancellationToken cancellationToken = default)
    {
        Seed(tagNamespace);
        return Task.FromResult(tagNamespace);
    }
}
