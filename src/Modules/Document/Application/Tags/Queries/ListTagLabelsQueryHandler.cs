using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.Document.Application.Tags.Repositories;
using ECM.Document.Application.Tags.Results;

namespace ECM.Document.Application.Tags.Queries;

public sealed class ListTagLabelsQueryHandler(ITagLabelRepository tagLabelRepository)
{
    private readonly ITagLabelRepository _tagLabelRepository = tagLabelRepository;

    public async Task<TagLabelResult[]> HandleAsync(CancellationToken cancellationToken = default)
    {
        var tagLabels = await _tagLabelRepository
            .ListWithNamespaceAsync(cancellationToken)
            .ConfigureAwait(false);

        return tagLabels
            .Select(label => new TagLabelResult(
                label.Id,
                label.NamespaceId,
                label.Namespace?.Scope ?? string.Empty,
                NormalizeNamespaceDisplayName(label.Namespace?.DisplayName, label.Namespace?.Scope),
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
            ))
            .ToArray();
    }

    private static string? NormalizeNamespaceDisplayName(string? displayName, string? fallback)
        => string.IsNullOrWhiteSpace(displayName) ? fallback : displayName.Trim();
}
