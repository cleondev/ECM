using System;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Domain.Documents;

namespace ECM.Document.Application.Documents.Commands;

public sealed class DeleteDocumentCommandHandler(
    IDocumentRepository repository,
    ISystemClock clock)
{
    private readonly IDocumentRepository _repository = repository;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult> HandleAsync(DeleteDocumentCommand command, CancellationToken cancellationToken = default)
    {
        var documentId = DocumentId.FromGuid(command.DocumentId);
        var document = await _repository.GetAsync(documentId, cancellationToken);

        if (document is null)
        {
            return OperationResult.Failure("Document not found.");
        }

        if (command.DeletedBy == Guid.Empty)
        {
            return OperationResult.Failure("Deleted by is required.");
        }

        var now = _clock.UtcNow;
        document.MarkDeleted(command.DeletedBy, now);
        await _repository.DeleteAsync(document, cancellationToken);

        return OperationResult.Success();
    }
}
