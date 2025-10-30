using System;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents.AccessControl;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Application.Documents.Summaries;
using ECM.Document.Domain.Documents;
using DocumentEntity = ECM.Document.Domain.Documents.Document;

namespace ECM.Document.Application.Documents.Commands;

public sealed class CreateDocumentCommandHandler(
    IDocumentRepository repository,
    ISystemClock clock,
    IEffectiveAclFlatWriter aclWriter)
{
    private readonly IDocumentRepository _repository = repository;
    private readonly ISystemClock _clock = clock;
    private readonly IEffectiveAclFlatWriter _aclWriter = aclWriter;

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
                command.GroupId,
                command.Sensitivity,
                command.DocumentTypeId);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<DocumentSummaryResult>.Failure(exception.Message);
        }

        document = await _repository.AddAsync(document, cancellationToken);

        var ownerEntry = EffectiveAclFlatWriteEntry.ForOwner(document.Id.Value, document.OwnerId);
        await _aclWriter.UpsertAsync(ownerEntry, cancellationToken);

        return OperationResult<DocumentSummaryResult>.Success(document.ToResult());
    }
}
