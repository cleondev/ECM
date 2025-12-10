using ECM.Document.Application.Tags.Repositories;
using ECM.Document.Application.Tags.Results;

namespace ECM.Document.Application.Tags.Queries;

public sealed class ListTagLabelsQueryHandler(ITagLabelRepository tagLabelRepository)
{
    private readonly ITagLabelRepository _tagLabelRepository = tagLabelRepository;

    public async Task<TagLabelResult[]> HandleAsync(
        Guid? ownerUserId,
        Guid? primaryGroupId,
        string? scope,
        bool includeAllNamespaces,
        CancellationToken cancellationToken = default)
    {
        var tagLabels = await _tagLabelRepository
            .ListWithNamespaceAsync(ownerUserId, primaryGroupId, scope, includeAllNamespaces, cancellationToken)
            .ConfigureAwait(false);

        return [.. tagLabels
            .Select(label => new TagLabelResult(
                label.Id,
                label.NamespaceId,
                NormalizeNamespaceScope(label.Namespace?.Scope),
                NormalizeNamespaceDisplayName(label.Namespace?.DisplayName),
                label.ParentId,
                label.Name,
                label.PathIds,
                label.SortOrder,
                label.Color,
                label.IconKey,
                label.IsActive,
                label.IsSystem,
                label.CreatedBy,
                label.CreatedAtUtc
            ))];
    }

    private static string? NormalizeNamespaceDisplayName(string? displayName)
        => string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();

    private static string? NormalizeNamespaceScope(string? scope)
        => string.IsNullOrWhiteSpace(scope) ? null : scope.Trim();
}
