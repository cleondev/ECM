using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Modules.Document.Domain.Documents;
using DocumentEntity = ECM.Modules.Document.Domain.Documents.Document;

namespace ECM.Modules.Document.Application.Documents;

public sealed class DocumentApplicationService
{
    private readonly IDocumentRepository _repository;
    private readonly ISystemClock _clock;

    public DocumentApplicationService(IDocumentRepository repository, ISystemClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<OperationResult<DocumentSummary>> CreateAsync(CreateDocumentCommand command, CancellationToken cancellationToken = default)
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

        var summary = new DocumentSummary(
            document.Id.Value,
            document.Title.Value,
            document.DocType,
            document.Status,
            document.Sensitivity,
            document.OwnerId,
            document.CreatedBy,
            document.Department,
            document.CreatedAtUtc,
            document.UpdatedAtUtc,
            document.TypeId);

        return OperationResult<DocumentSummary>.Success(summary);
    }
}
