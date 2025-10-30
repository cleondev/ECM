using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Tags.Repositories;
using ECM.Document.Application.Tags.Results;
using ECM.Document.Domain.Tags;

namespace ECM.Document.Application.Tags.Commands;

public sealed class CreateTagLabelCommandHandler(
    ITagLabelRepository tagLabelRepository,
    ITagNamespaceRepository tagNamespaceRepository,
    ISystemClock clock)
{
    private readonly ITagLabelRepository _tagLabelRepository = tagLabelRepository;
    private readonly ITagNamespaceRepository _tagNamespaceRepository = tagNamespaceRepository;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<TagLabelResult>> HandleAsync(
        CreateTagLabelCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.NamespaceId == Guid.Empty)
        {
            return OperationResult<TagLabelResult>.Failure("Namespace identifier is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return OperationResult<TagLabelResult>.Failure("Tag name is required.");
        }

        if (command.CreatedBy is null || command.CreatedBy == Guid.Empty)
        {
            return OperationResult<TagLabelResult>.Failure("A valid creator identifier is required.");
        }

        var tagNamespace = await _tagNamespaceRepository
            .GetAsync(command.NamespaceId, cancellationToken)
            .ConfigureAwait(false);

        if (tagNamespace is null)
        {
            return OperationResult<TagLabelResult>.Failure("Tag namespace does not exist.");
        }

        var namespaceDisplayName = string.IsNullOrWhiteSpace(tagNamespace.DisplayName)
            ? tagNamespace.Scope
            : tagNamespace.DisplayName.Trim();
        var namespaceScope = tagNamespace.Scope;

        var normalizedName = command.Name.Trim();
        var parentId = NormalizeGuid(command.ParentId);

        TagLabel? parent = null;
        if (parentId is not null)
        {
            parent = await _tagLabelRepository
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
        }

        var duplicateExists = await _tagLabelRepository
            .ExistsWithNameAsync(
                command.NamespaceId,
                parentId,
                normalizedName,
                excludeTagId: null,
                cancellationToken)
            .ConfigureAwait(false);

        if (duplicateExists)
        {
            return OperationResult<TagLabelResult>.Failure("A tag with the same name already exists at this level.");
        }

        var createdAt = _clock.UtcNow;
        var parentPath = parent?.PathIds ?? [];
        var tagLabel = TagLabel.Create(
            command.NamespaceId,
            parentId,
            parentPath,
            normalizedName,
            command.SortOrder ?? 0,
            command.Color,
            command.IconKey,
            command.CreatedBy,
            command.IsSystem,
            createdAt);

        await _tagLabelRepository.AddAsync(tagLabel, cancellationToken).ConfigureAwait(false);

        var result = new TagLabelResult(
            tagLabel.Id,
            tagLabel.NamespaceId,
            namespaceScope,
            namespaceDisplayName,
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

    private static Guid? NormalizeGuid(Guid? value)
        => value.HasValue && value.Value != Guid.Empty ? value : null;
}
