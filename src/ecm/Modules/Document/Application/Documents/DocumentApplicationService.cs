using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Modules.Document.Domain.Documents;

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

        var document = Document.Create(title, _clock.UtcNow);
        document = await _repository.AddAsync(document, cancellationToken);

        var summary = new DocumentSummary(document.Id.Value, document.Title.Value, document.CreatedAtUtc);
        return OperationResult<DocumentSummary>.Success(summary);
    }
}
