using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Domain.Documents;

using DomainDocument = ECM.Document.Domain.Documents.Document;

namespace ECM.Document.Application.Documents.Commands;

public sealed class UpdateDocumentCommandHandler(
    IDocumentRepository repository,
    ISystemClock clock)
{
    private const string DocumentNotFoundError = "Document not found.";

    private readonly IDocumentRepository _repository = repository;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<DomainDocument>> HandleAsync(
        UpdateDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        var documentId = DocumentId.FromGuid(command.DocumentId);
        var document = await _repository.GetAsync(documentId, cancellationToken);

        if (document is null)
        {
            return OperationResult<DomainDocument>.Failure(DocumentNotFoundError);
        }

        var now = _clock.UtcNow;

        if (command.Title is not null)
        {
            try
            {
                var title = DocumentTitle.Create(command.Title);
                document.UpdateTitle(title, now);
            }
            catch (ArgumentException exception)
            {
                return OperationResult<DomainDocument>.Failure(exception.Message);
            }
        }

        if (command.Status is not null)
        {
            try
            {
                document.UpdateStatus(command.Status, now);
            }
            catch (ArgumentException exception)
            {
                return OperationResult<DomainDocument>.Failure(exception.Message);
            }
        }

        if (command.Sensitivity is not null)
        {
            try
            {
                document.UpdateSensitivity(command.Sensitivity, now);
            }
            catch (ArgumentException exception)
            {
                return OperationResult<DomainDocument>.Failure(exception.Message);
            }
        }

        if (command.HasGroupId)
        {
            document.UpdateGroupId(command.GroupId, now);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return OperationResult<DomainDocument>.Success(document);
    }

    public static bool IsNotFound(OperationResult<DomainDocument> result)
    {
        if (!result.IsFailure)
        {
            return false;
        }

        return result.Errors.Any(error => string.Equals(error, DocumentNotFoundError, StringComparison.Ordinal));
    }
}
