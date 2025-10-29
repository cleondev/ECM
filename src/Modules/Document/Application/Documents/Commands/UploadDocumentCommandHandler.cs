using System;
using System.Collections.Generic;
using System.Linq;
using ECM.Abstractions.Files;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Application.Documents.Summaries;
using ECM.Document.Domain.Documents;
using ECM.Document.Domain.Versions;
using DocumentEntity = ECM.Document.Domain.Documents.Document;

namespace ECM.Document.Application.Documents.Commands;

public sealed class UploadDocumentCommandHandler(
    IDocumentRepository repository,
    IFileStorageGateway fileStorage,
    ISystemClock clock)
{
    private readonly IDocumentRepository _repository = repository;
    private readonly IFileStorageGateway _fileStorage = fileStorage;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<DocumentWithVersionResult>> HandleAsync(UploadDocumentCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.FileSize <= 0)
        {
            return OperationResult<DocumentWithVersionResult>.Failure("File size must be greater than zero.");
        }

        DocumentTitle title;
        try
        {
            title = DocumentTitle.Create(command.Title);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<DocumentWithVersionResult>.Failure(exception.Message);
        }

        var now = _clock.UtcNow;

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
                now,
                primaryGroupId,
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
                command.CreatedBy,
                now);
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
        {
            return OperationResult<DocumentWithVersionResult>.Failure(exception.Message);
        }

        await _repository.AddAsync(document, cancellationToken);

        return OperationResult<DocumentWithVersionResult>.Success(
            document.ToResult(version));
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
