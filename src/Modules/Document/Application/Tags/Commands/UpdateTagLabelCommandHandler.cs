using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Tags.Repositories;
using ECM.Document.Application.Tags.Results;
using ECM.Document.Domain.Tags;

namespace ECM.Document.Application.Tags.Commands;

public sealed class UpdateTagLabelCommandHandler(
    ITagLabelRepository tagLabelRepository,
    ISystemClock clock)
{
    private const string TagNotFoundError = "Tag label not found.";
    private static readonly Regex SlugPattern = new("^[a-z0-9_]+(-[a-z0-9_]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ITagLabelRepository _tagLabelRepository = tagLabelRepository;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<TagLabelResult>> HandleAsync(UpdateTagLabelCommand command, CancellationToken cancellationToken = default)
    {
        if (command.TagId == Guid.Empty)
        {
            return OperationResult<TagLabelResult>.Failure("Tag identifier is required.");
        }

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

        var tagLabel = await _tagLabelRepository.GetByIdAsync(command.TagId, cancellationToken).ConfigureAwait(false);
        if (tagLabel is null)
        {
            return OperationResult<TagLabelResult>.Failure(TagNotFoundError);
        }

        if (!string.Equals(tagLabel.NamespaceSlug, namespaceSlug, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<TagLabelResult>.Failure("Namespace slug cannot be changed.");
        }

        var existingTag = await _tagLabelRepository
            .GetByNamespaceAndPathAsync(namespaceSlug, path, cancellationToken)
            .ConfigureAwait(false);

        if (existingTag is not null && existingTag.Id != tagLabel.Id)
        {
            return OperationResult<TagLabelResult>.Failure("Tag with the provided namespace and path already exists.");
        }

        var updatedAtUtc = _clock.UtcNow;
        tagLabel.Update(slug, path, command.UpdatedBy, updatedAtUtc);
        await _tagLabelRepository.UpdateAsync(tagLabel, cancellationToken).ConfigureAwait(false);

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

    public static bool IsNotFound(OperationResult<TagLabelResult> result)
        => result.IsFailure && result.Errors.Any(error => string.Equals(error, TagNotFoundError, StringComparison.Ordinal));
}
