using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Domain.Tags;

namespace ECM.Document.Application.Tags;

public sealed class CreateTagLabelCommandHandler(
    ITagLabelRepository tagLabelRepository,
    ISystemClock clock)
{
    private static readonly Regex SlugPattern = new("^[a-z0-9_]+(-[a-z0-9_]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ITagLabelRepository _tagLabelRepository = tagLabelRepository;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<TagLabelResult>> HandleAsync(CreateTagLabelCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.NamespaceSlug))
        {
            return OperationResult<TagLabelResult>.Failure("Namespace slug is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Slug))
        {
            return OperationResult<TagLabelResult>.Failure("Tag slug is required.");
        }

        var namespaceSlug = command.NamespaceSlug.Trim().ToLowerInvariant();
        var slug = command.Slug.Trim().ToLowerInvariant();
        var path = string.IsNullOrWhiteSpace(command.Path)
            ? slug
            : command.Path.Trim().ToLowerInvariant();

        if (!SlugPattern.IsMatch(slug))
        {
            return OperationResult<TagLabelResult>.Failure("Slug must contain lowercase letters, numbers, underscores or hyphens.");
        }

        if (!SlugPattern.IsMatch(path))
        {
            return OperationResult<TagLabelResult>.Failure("Path must contain lowercase letters, numbers, underscores or hyphens.");
        }

        if (!await _tagLabelRepository.NamespaceExistsAsync(namespaceSlug, cancellationToken).ConfigureAwait(false))
        {
            return OperationResult<TagLabelResult>.Failure("Tag namespace does not exist.");
        }

        var existingTag = await _tagLabelRepository
            .GetByNamespaceAndPathAsync(namespaceSlug, path, cancellationToken)
            .ConfigureAwait(false);

        if (existingTag is not null)
        {
            return OperationResult<TagLabelResult>.Failure("Tag with the provided namespace and path already exists.");
        }

        var createdAt = _clock.UtcNow;
        var tagLabel = TagLabel.Create(namespaceSlug, slug, path, command.CreatedBy, createdAt);
        await _tagLabelRepository.AddAsync(tagLabel, cancellationToken).ConfigureAwait(false);

        var result = new TagLabelResult(
            tagLabel.Id,
            tagLabel.NamespaceSlug,
            tagLabel.Slug,
            tagLabel.Path,
            tagLabel.IsActive,
            tagLabel.CreatedBy,
            tagLabel.CreatedAtUtc);

        return OperationResult<TagLabelResult>.Success(result);
    }
}
