using System;
using System.ComponentModel.DataAnnotations;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents.AccessControl;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Application.Documents.Summaries;
using ECM.Document.Application.UserContext;
using DocumentEntity = ECM.Document.Domain.Documents.Document;

namespace ECM.Document.Application.Documents.Commands;

public sealed class CreateDocumentCommandHandler(
    IDocumentRepository repository,
    ISystemClock clock,
    IEffectiveAclFlatWriter aclWriter,
    IDocumentUserContextResolver userContextResolver)
{
    private readonly IDocumentRepository _repository = repository;
    private readonly ISystemClock _clock = clock;
    private readonly IEffectiveAclFlatWriter _aclWriter = aclWriter;
    private readonly IDocumentUserContextResolver _userContextResolver = userContextResolver;

    public async Task<OperationResult<DocumentSummaryResult>> HandleAsync(CreateDocumentCommand command, CancellationToken cancellationToken = default)
    {
        DocumentUserContext userContext;
        try
        {
            userContext = _userContextResolver.Resolve(new DocumentCommandContext(command.OwnerId, command.CreatedBy));
        }
        catch (ValidationException exception)
        {
            return OperationResult<DocumentSummaryResult>.Failure(exception.Message);
        }

        DocumentEntity document;
        try
        {
            document = DocumentEntity.Create(
                command.Title,
                command.DocType,
                command.Status,
                userContext.OwnerId,
                userContext.CreatedBy,
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

        var ownerEntry = EffectiveAclFlatWriteEntry.ForOwner(document.Id, document.OwnerId);
        await _aclWriter.UpsertAsync(ownerEntry, cancellationToken);

        return OperationResult<DocumentSummaryResult>.Success(document.ToResult());
    }
}
