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

        if (command.UpdatedBy == Guid.Empty)
        {
            return OperationResult<DomainDocument>.Failure("Updated by is required.");
        }

        var now = _clock.UtcNow;
        var hasChanges = false;

        if (command.Title is not null)
        {
            try
            {
                var title = DocumentTitle.Create(command.Title);
                if (!string.Equals(document.Title.Value, title.Value, StringComparison.Ordinal))
                {
                    document.UpdateTitle(title, now);
                    hasChanges = true;
                }
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
                if (string.IsNullOrWhiteSpace(command.Status))
                {
                    document.UpdateStatus(command.Status, now);
                }
                else
                {
                    var trimmedStatus = command.Status.Trim();
                    if (!string.Equals(document.Status, trimmedStatus, StringComparison.Ordinal))
                    {
                        document.UpdateStatus(command.Status, now);
                        hasChanges = true;
                    }
                }
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
                if (string.IsNullOrWhiteSpace(command.Sensitivity))
                {
                    document.UpdateSensitivity(command.Sensitivity, now);
                }
                else
                {
                    var trimmedSensitivity = command.Sensitivity.Trim();
                    if (!string.Equals(document.Sensitivity, trimmedSensitivity, StringComparison.Ordinal))
                    {
                        document.UpdateSensitivity(command.Sensitivity, now);
                        hasChanges = true;
                    }
                }
            }
            catch (ArgumentException exception)
            {
                return OperationResult<DomainDocument>.Failure(exception.Message);
            }
        }

        if (command.HasGroupId)
        {
            var normalizedGroupId = NormalizeGroupId(command.GroupId);
            if (normalizedGroupId != document.GroupId)
            {
                document.UpdateGroupId(command.GroupId, now);
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            document.MarkUpdated(command.UpdatedBy, now);
            await _repository.SaveChangesAsync(cancellationToken);
        }

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

    private static Guid? NormalizeGroupId(Guid? groupId)
    {
        return groupId is null || groupId == Guid.Empty ? null : groupId;
    }
}
