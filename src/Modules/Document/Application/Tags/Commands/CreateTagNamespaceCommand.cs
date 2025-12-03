using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Tags.Repositories;
using ECM.Document.Domain.Tags;

namespace ECM.Document.Application.Tags.Commands;

public sealed record CreateTagNamespaceCommand(
    string Scope,
    Guid? OwnerUserId,
    Guid? OwnerGroupId,
    string? DisplayName,
    Guid? CreatedBy);

public sealed class CreateTagNamespaceCommandHandler(
    ITagNamespaceRepository tagNamespaceRepository,
    ISystemClock clock)
{
    private readonly ITagNamespaceRepository _tagNamespaceRepository = tagNamespaceRepository;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<TagNamespace>> HandleAsync(
        CreateTagNamespaceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Scope))
        {
            return OperationResult<TagNamespace>.Failure("Namespace scope is required.");
        }

        var normalizedScope = command.Scope.Trim().ToLowerInvariant();

        if (normalizedScope != "global" && normalizedScope != "group" && normalizedScope != "user")
        {
            return OperationResult<TagNamespace>.Failure("Namespace scope must be one of: global, group, user.");
        }

        if (command.CreatedBy is null || command.CreatedBy == Guid.Empty)
        {
            return OperationResult<TagNamespace>.Failure("A valid creator identifier is required.");
        }

        if (normalizedScope == "group" && (!command.OwnerGroupId.HasValue || command.OwnerGroupId == Guid.Empty))
        {
            return OperationResult<TagNamespace>.Failure("Group namespaces require an owning group identifier.");
        }

        if (normalizedScope == "user" && (!command.OwnerUserId.HasValue || command.OwnerUserId == Guid.Empty))
        {
            return OperationResult<TagNamespace>.Failure("User namespaces require an owning user identifier.");
        }

        var namespaceEntity = TagNamespace.Create(
            normalizedScope,
            command.OwnerUserId,
            command.OwnerGroupId,
            command.DisplayName,
            isSystem: false,
            _clock.UtcNow);

        await _tagNamespaceRepository.AddAsync(namespaceEntity, cancellationToken).ConfigureAwait(false);

        return OperationResult<TagNamespace>.Success(namespaceEntity);
    }
}
