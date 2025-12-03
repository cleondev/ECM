using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.Document.Application.Tags.Repositories;
using ECM.Document.Domain.Tags;

namespace ECM.Document.Application.Tags.Commands;

public sealed record DeleteTagNamespaceCommand(Guid NamespaceId);

public sealed class DeleteTagNamespaceCommandHandler(
    ITagNamespaceRepository tagNamespaceRepository,
    ITagLabelRepository tagLabelRepository)
{
    private readonly ITagNamespaceRepository _tagNamespaceRepository = tagNamespaceRepository;
    private readonly ITagLabelRepository _tagLabelRepository = tagLabelRepository;

    public async Task<OperationResult<bool>> HandleAsync(
        DeleteTagNamespaceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.NamespaceId == Guid.Empty)
        {
            return OperationResult<bool>.Failure("Namespace identifier is required.");
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
            return OperationResult<bool>.Failure("System namespaces cannot be deleted.");
        }

        if (await _tagLabelRepository.AnyInNamespaceAsync(command.NamespaceId, cancellationToken).ConfigureAwait(false))
        {
            return OperationResult<bool>.Failure("Remove or move tags before deleting this namespace.");
        }

        await _tagNamespaceRepository.RemoveAsync(namespaceEntity, cancellationToken).ConfigureAwait(false);

        return OperationResult<bool>.Success(true);
    }
}
