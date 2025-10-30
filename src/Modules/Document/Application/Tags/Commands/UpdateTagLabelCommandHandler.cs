using System;
using System.Linq;
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
    ITagNamespaceRepository tagNamespaceRepository,
    ISystemClock clock)
{
    public const string TagNotFoundError = "Tag label was not found.";

    private readonly ITagLabelRepository _tagLabelRepository = tagLabelRepository;
    private readonly ITagNamespaceRepository _tagNamespaceRepository = tagNamespaceRepository;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<TagLabelResult>> HandleAsync(
        UpdateTagLabelCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.TagId == Guid.Empty)
        {
            return OperationResult<TagLabelResult>.Failure("Tag identifier is required.");
        }

        if (command.NamespaceId == Guid.Empty)
        {
            return OperationResult<TagLabelResult>.Failure("Namespace identifier is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return OperationResult<TagLabelResult>.Failure("Tag name is required.");
        }

        var tagLabel = await _tagLabelRepository
            .GetByIdAsync(command.TagId, cancellationToken)
            .ConfigureAwait(false);

        if (tagLabel is null)
        {
            return OperationResult<TagLabelResult>.Failure(TagNotFoundError);
        }

        if (tagLabel.IsSystem)
        {
            return OperationResult<TagLabelResult>.Failure("System tags cannot be modified.");
        }

        if (tagLabel.NamespaceId != command.NamespaceId)
        {
            return OperationResult<TagLabelResult>.Failure("Tag belongs to a different namespace.");
        }

        var namespaceExists = await _tagNamespaceRepository
            .GetAsync(command.NamespaceId, cancellationToken)
            .ConfigureAwait(false);

        if (namespaceExists is null)
        {
            return OperationResult<TagLabelResult>.Failure("Tag namespace does not exist.");
        }

        var normalizedName = command.Name.Trim();
        var parentId = NormalizeGuid(command.ParentId);
        var parentPath = Array.Empty<Guid>();

        if (parentId.HasValue)
        {
            if (parentId.Value == command.TagId)
            {
                return OperationResult<TagLabelResult>.Failure("A tag cannot be its own parent.");
            }

            var parent = await _tagLabelRepository
                .GetByIdAsync(parentId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (parent is null)
            {
                return OperationResult<TagLabelResult>.Failure("Parent tag was not found.");
            }

            if (parent.NamespaceId != command.NamespaceId)
            {
                return OperationResult<TagLabelResult>.Failure("Parent tag belongs to a different namespace.");
            }

            if (parent.PathIds.Contains(command.TagId))
            {
                return OperationResult<TagLabelResult>.Failure("Cannot assign a descendant as the parent.");
            }

            parentPath = parent.PathIds;
        }

        var duplicateExists = await _tagLabelRepository
            .ExistsWithNameAsync(
                command.NamespaceId,
                parentId,
                normalizedName,
                excludeTagId: command.TagId,
                cancellationToken)
            .ConfigureAwait(false);

        if (duplicateExists)
        {
            return OperationResult<TagLabelResult>.Failure("A tag with the same name already exists at this level.");
        }

        var updatedAt = _clock.UtcNow;

        tagLabel.Update(
            normalizedName,
            parentId,
            parentId is null ? Array.Empty<Guid>() : parentPath,
            command.SortOrder ?? tagLabel.SortOrder,
            command.Color,
            command.IconKey,
            command.IsActive,
            NormalizeGuid(command.UpdatedBy),
            updatedAt);

        await _tagLabelRepository.UpdateAsync(tagLabel, cancellationToken).ConfigureAwait(false);

        var result = new TagLabelResult(
            tagLabel.Id,
            tagLabel.NamespaceId,
            tagLabel.ParentId,
            tagLabel.Name,
            tagLabel.PathIds,
            tagLabel.SortOrder,
            tagLabel.Color,
            tagLabel.IconKey,
            tagLabel.IsActive,
            tagLabel.IsSystem,
            tagLabel.CreatedBy,
            tagLabel.CreatedAtUtc);

        return OperationResult<TagLabelResult>.Success(result);
    }

    public static bool IsNotFound(OperationResult<TagLabelResult> result)
        => result.IsFailure && result.Errors.Any(error => string.Equals(error, TagNotFoundError, StringComparison.Ordinal));

    private static Guid? NormalizeGuid(Guid? value)
        => value.HasValue && value.Value != Guid.Empty ? value : null;
}
