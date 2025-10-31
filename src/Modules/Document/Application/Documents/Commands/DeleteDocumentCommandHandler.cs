using ECM.BuildingBlocks.Application;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Domain.Documents;

namespace ECM.Document.Application.Documents.Commands;

public sealed class DeleteDocumentCommandHandler(IDocumentRepository repository)
{
    private readonly IDocumentRepository _repository = repository;

    public async Task<OperationResult> HandleAsync(DeleteDocumentCommand command, CancellationToken cancellationToken = default)
    {
        var documentId = DocumentId.FromGuid(command.DocumentId);
        var document = await _repository.GetAsync(documentId, cancellationToken);

        if (document is null)
        {
            return OperationResult.Failure("Document not found.");
        }

        await _repository.DeleteAsync(document, cancellationToken);

        return OperationResult.Success();
    }
}
