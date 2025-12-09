using System;
using System.ComponentModel.DataAnnotations;
using ECM.Abstractions.Files;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Application.Documents.Summaries;
using ECM.Document.Application.UserContext;
using ECM.Document.Domain.Documents;
using ECM.Document.Domain.Versions;

namespace ECM.Document.Application.Documents.Commands;

public sealed class UploadDocumentVersionCommandHandler(
    IDocumentRepository repository,
    IFileStorageGateway fileStorage,
    ISystemClock clock,
    IDocumentUserContextResolver userContextResolver)
{
    internal const string DocumentNotFoundError = "Document not found.";

    private readonly IDocumentRepository _repository = repository;
    private readonly IFileStorageGateway _fileStorage = fileStorage;
    private readonly ISystemClock _clock = clock;
    private readonly IDocumentUserContextResolver _userContextResolver = userContextResolver;

    public async Task<OperationResult<DocumentVersionResult>> HandleAsync(
        UploadDocumentVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.FileSize <= 0)
        {
            return OperationResult<DocumentVersionResult>.Failure("File size must be greater than zero.");
        }

        var documentId = DocumentId.FromGuid(command.DocumentId);
        var document = await _repository.GetAsync(documentId, cancellationToken);

        if (document is null)
        {
            return OperationResult<DocumentVersionResult>.Failure(DocumentNotFoundError);
        }

        DocumentUserContext userContext;
        try
        {
            userContext = _userContextResolver.Resolve(new DocumentCommandContext(document.OwnerId, command.CreatedBy));
        }
        catch (ValidationException exception)
        {
            return OperationResult<DocumentVersionResult>.Failure(exception.Message);
        }

        var uploadRequest = new FileUploadRequest(
            command.FileName,
            command.ContentType,
            command.FileSize,
            command.Content);

        var uploadResult = await _fileStorage.UploadAsync(uploadRequest, cancellationToken);
        if (uploadResult.IsFailure || uploadResult.Value is null)
        {
            return OperationResult<DocumentVersionResult>.Failure([.. uploadResult.Errors]);
        }

        var now = _clock.UtcNow;
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
            return OperationResult<DocumentVersionResult>.Failure(exception.Message);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return OperationResult<DocumentVersionResult>.Success(version.ToResult());
    }
}
