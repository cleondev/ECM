using System;
using System.Collections.Generic;
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

    public async Task<OperationResult<DocumentSummaryResult>> HandleAsync(CreateDocumentCommand command, CancellationToken cancellationToken = default)
    {
        DocumentTitle title;
        try
        {
            title = DocumentTitle.Create(command.Title);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<DocumentSummaryResult>.Failure(exception.Message);
        }

        var primaryGroupId = ResolvePrimaryGroupId(command.GroupId, command.GroupIds);

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
                primaryGroupId,
                command.Sensitivity,
                command.DocumentTypeId);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<DocumentSummaryResult>.Failure(exception.Message);
        }

        document = await _repository.AddAsync(document, cancellationToken);

        return OperationResult<DocumentSummaryResult>.Success(document.ToResult());
    }

    private static Guid? ResolvePrimaryGroupId(Guid? groupId, IReadOnlyCollection<Guid> groupIds)
    {
        if (groupId.HasValue && groupId.Value != Guid.Empty)
        {
            return groupId.Value;
        }

        if (groupIds is not null)
        {
            foreach (var id in groupIds)
            {
                if (id != Guid.Empty)
                {
                    return id;
                }
            }
        }

        return null;
    }
}
