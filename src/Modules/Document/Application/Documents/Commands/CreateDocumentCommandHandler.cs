using System;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Application.Documents.Summaries;
using ECM.Document.Domain.Documents;
using DocumentEntity = ECM.Document.Domain.Documents.Document;

namespace ECM.Document.Application.Documents.Commands;

public sealed class CreateDocumentCommandHandler(IDocumentRepository repository, ISystemClock clock)
{
    private readonly IDocumentRepository _repository = repository;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<DocumentSummary>> HandleAsync(CreateDocumentCommand command, CancellationToken cancellationToken = default)
    {
        DocumentTitle title;
        try
        {
            title = DocumentTitle.Create(command.Title);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<DocumentSummary>.Failure(exception.Message);
        }

        DocumentEntity document;
        try
        {
            document = DocumentEntity.Create(
                title,
                command.DocType,
                command.Status,
                command.OwnerId,
                command.CreatedBy,
                _clock.UtcNow,
                command.Department,
                command.Sensitivity,
                command.DocumentTypeId);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<DocumentSummary>.Failure(exception.Message);
        }

        document = await _repository.AddAsync(document, cancellationToken);

        return OperationResult<DocumentSummary>.Success(DocumentSummaryMapper.ToSummary(document));
    }
}
