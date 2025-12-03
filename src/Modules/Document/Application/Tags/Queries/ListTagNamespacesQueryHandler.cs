using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.Document.Application.Tags.Repositories;
using ECM.Document.Domain.Tags;

namespace ECM.Document.Application.Tags.Queries;

public sealed class ListTagNamespacesQueryHandler(ITagNamespaceRepository tagNamespaceRepository)
{
    private readonly ITagNamespaceRepository _tagNamespaceRepository = tagNamespaceRepository;

    public async Task<TagNamespace[]> HandleAsync(
        Guid? ownerUserId,
        Guid? primaryGroupId,
        string? scope,
        bool includeAll,
        CancellationToken cancellationToken = default)
    {
        var normalizedScope = NormalizeScope(scope);

        if (includeAll)
        {
            return await _tagNamespaceRepository.ListAsync(normalizedScope, cancellationToken).ConfigureAwait(false);
        }

        var allowedScopes = normalizedScope is null or "all"
            ? new[] { "global", "group", "user" }
            : new[] { normalizedScope };

        var namespaces = await _tagNamespaceRepository.ListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        return namespaces
            .Where(ns =>
                allowedScopes.Contains(ns.Scope)
                && (ns.Scope == "global"
                    || (ns.Scope == "user" && ownerUserId.HasValue && ns.OwnerUserId == ownerUserId)
                    || (ns.Scope == "group" && primaryGroupId.HasValue && ns.OwnerGroupId == primaryGroupId)))
            .ToArray();
    }

    private static string? NormalizeScope(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return null;
        }

        var normalized = scope.Trim().ToLowerInvariant();
        return normalized == "all" ? null : normalized;
    }
}
