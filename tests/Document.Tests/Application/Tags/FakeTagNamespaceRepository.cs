using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.Document.Application.Tags.Repositories;

namespace Document.Tests.Application.Tags;

internal sealed class FakeTagNamespaceRepository : ITagNamespaceRepository
{
    private readonly HashSet<string> _namespaces = new(StringComparer.OrdinalIgnoreCase);

    public FakeTagNamespaceRepository(IEnumerable<string>? namespaces = null)
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

    public IReadOnlyCollection<string> Namespaces => _namespaces;

    public Task<bool> ExistsAsync(string namespaceSlug, CancellationToken cancellationToken = default)
        => Task.FromResult(_namespaces.Contains(namespaceSlug));

    public Task EnsureUserNamespaceAsync(
        string namespaceSlug,
        Guid? ownerUserId,
        string? displayName,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        _namespaces.Add(namespaceSlug);
        return Task.CompletedTask;
    }
}
