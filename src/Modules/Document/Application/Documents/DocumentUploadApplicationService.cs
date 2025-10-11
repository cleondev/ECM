using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.Abstractions.Files;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Domain.Documents;
using ECM.Document.Domain.Versions;
using DocumentEntity = ECM.Document.Domain.Documents.Document;

namespace ECM.Document.Application.Documents;

public sealed class DocumentUploadApplicationService(
    IDocumentRepository repository,
    IFileUploadService fileUploadService,
    ISystemClock clock)
{
    private readonly IDocumentRepository _repository = repository;
    private readonly IFileUploadService _fileUploadService = fileUploadService;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<DocumentWithVersionSummary>> CreateAsync(UploadDocumentCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (command.FileSize <= 0)
        {
            return OperationResult<DocumentWithVersionSummary>.Failure("File size must be greater than zero.");
        }

        DocumentTitle title;
        try
        {
            title = DocumentTitle.Create(command.Title);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<DocumentWithVersionSummary>.Failure(exception.Message);
        }

        var now = _clock.UtcNow;

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
                command.Department,
                command.Sensitivity,
                command.DocumentTypeId);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<DocumentWithVersionSummary>.Failure(exception.Message);
        }

        var uploadRequest = new FileUploadRequest(
            command.FileName,
            command.ContentType,
            command.FileSize,
            command.Content);

        var uploadResult = await _fileUploadService.UploadAsync(uploadRequest, cancellationToken);
        if (uploadResult.IsFailure || uploadResult.Value is null)
        {
            return OperationResult<DocumentWithVersionSummary>.Failure(uploadResult.Errors.ToArray());
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
            return OperationResult<DocumentWithVersionSummary>.Failure(exception.Message);
        }

        await _repository.AddAsync(document, cancellationToken);

        return OperationResult<DocumentWithVersionSummary>.Success(
            DocumentSummaryMapper.ToSummary(document, version));
    }
}
