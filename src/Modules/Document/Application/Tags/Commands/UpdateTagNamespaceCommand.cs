using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.Document.Application.Tags.Repositories;

namespace ECM.Document.Application.Tags.Commands;

public sealed record UpdateTagNamespaceCommand(
    Guid NamespaceId,
    string? DisplayName,
    Guid? UpdatedBy);

public sealed class UpdateTagNamespaceCommandHandler(ITagNamespaceRepository tagNamespaceRepository)
{
    private readonly ITagNamespaceRepository _tagNamespaceRepository = tagNamespaceRepository;

    public async Task<OperationResult<bool>> HandleAsync(
        UpdateTagNamespaceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.NamespaceId == Guid.Empty)
        {
            return OperationResult<bool>.Failure("Namespace identifier is required.");
        }

        if (command.UpdatedBy is null || command.UpdatedBy == Guid.Empty)
        {
            return OperationResult<bool>.Failure("A valid updater identifier is required.");
        }

        var namespaceEntity = await _tagNamespaceRepository
            .GetAsync(command.NamespaceId, cancellationToken)
            .ConfigureAwait(false);

        if (namespaceEntity is null)
        {
            return OperationResult<bool>.Failure("Tag namespace was not found.");
        }

        if (namespaceEntity.IsSystem)
        {
            return OperationResult<bool>.Failure("System namespaces cannot be modified.");
        }

        namespaceEntity.Rename(command.DisplayName);

        await _tagNamespaceRepository.UpdateAsync(namespaceEntity, cancellationToken).ConfigureAwait(false);

        return OperationResult<bool>.Success(true);
    }
}
