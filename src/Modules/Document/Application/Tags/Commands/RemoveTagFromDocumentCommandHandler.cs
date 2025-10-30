using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Application.Tags.Repositories;
using ECM.Document.Domain.Documents;

namespace ECM.Document.Application.Tags.Commands;

public sealed class RemoveTagFromDocumentCommandHandler(
    ITagLabelRepository tagLabelRepository,
    IDocumentRepository documentRepository,
    ISystemClock clock)
{
    private readonly ITagLabelRepository _tagLabelRepository = tagLabelRepository;
    private readonly IDocumentRepository _documentRepository = documentRepository;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<bool>> HandleAsync(RemoveTagFromDocumentCommand command, CancellationToken cancellationToken = default)
    {
        if (command.DocumentId == Guid.Empty)
        {
            return OperationResult<bool>.Failure("Document identifier is required.");
        }

        if (command.TagId == Guid.Empty)
        {
            return OperationResult<bool>.Failure("Tag identifier is required.");
        }

        var documentId = DocumentId.FromGuid(command.DocumentId);
        var document = await _documentRepository.GetAsync(documentId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return OperationResult<bool>.Failure("Document was not found.");
        }

        bool removed;
        try
        {
            removed = document.RemoveTag(command.TagId, _clock.UtcNow);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<bool>.Failure(exception.Message);
        }

        if (!removed)
        {
            return OperationResult<bool>.Failure("Tag label is not assigned to the document.");
        }

        await _documentRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return OperationResult<bool>.Success(true);
    }
}
