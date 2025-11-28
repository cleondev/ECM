using System;
using System.ComponentModel.DataAnnotations;
using ECM.Abstractions.Files;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents.AccessControl;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Application.Documents.Summaries;
using ECM.Document.Application.UserContext;
using ECM.Document.Domain.Versions;
using DocumentEntity = ECM.Document.Domain.Documents.Document;

namespace ECM.Document.Application.Documents.Commands;

public sealed class UploadDocumentCommandHandler(
    IDocumentRepository repository,
    IFileStorageGateway fileStorage,
    ISystemClock clock,
    IEffectiveAclFlatWriter aclWriter,
    IDocumentUserContextResolver userContextResolver)
{
    private readonly IDocumentRepository _repository = repository;
    private readonly IFileStorageGateway _fileStorage = fileStorage;
    private readonly ISystemClock _clock = clock;
    private readonly IEffectiveAclFlatWriter _aclWriter = aclWriter;
    private readonly IDocumentUserContextResolver _userContextResolver = userContextResolver;

    public async Task<OperationResult<DocumentWithVersionResult>> HandleAsync(UploadDocumentCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.FileSize <= 0)
        {
            return OperationResult<DocumentWithVersionResult>.Failure("File size must be greater than zero.");
        }

        var now = _clock.UtcNow;
        DocumentUserContext userContext;

        try
        {
            userContext = _userContextResolver.Resolve(new DocumentCommandContext(command.OwnerId, command.CreatedBy));
        }
        catch (ValidationException exception)
        {
            return OperationResult<DocumentWithVersionResult>.Failure(exception.Message);
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
                now,
                command.GroupId,
                command.Sensitivity,
                command.DocumentTypeId);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<DocumentWithVersionResult>.Failure(exception.Message);
        }

        var uploadRequest = new FileUploadRequest(
            command.FileName,
            command.ContentType,
            command.FileSize,
            command.Content);

        var uploadResult = await _fileStorage.UploadAsync(uploadRequest, cancellationToken);
        if (uploadResult.IsFailure || uploadResult.Value is null)
        {
            return OperationResult<DocumentWithVersionResult>.Failure([.. uploadResult.Errors]);
        }

        DocumentVersion version;
        try
        {
            version = document.AddVersion(
                uploadResult.Value.StorageKey,
                uploadResult.Value.Length,
                uploadResult.Value.ContentType,
                command.Sha256,
                userContext.CreatedBy,
                now);
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
        {
            return OperationResult<DocumentWithVersionResult>.Failure(exception.Message);
        }

        await _repository.AddAsync(document, cancellationToken);

        var ownerEntry = EffectiveAclFlatWriteEntry.ForOwner(document.Id, document.OwnerId);
        await _aclWriter.UpsertAsync(ownerEntry, cancellationToken);

        return OperationResult<DocumentWithVersionResult>.Success(
            document.ToResult(version));
    }

}
