using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Application.Tags.Repositories;
using ECM.Document.Domain.Documents;

namespace ECM.Document.Application.Tags.Commands;

public sealed class AssignTagToDocumentCommandHandler(
    ITagLabelRepository tagLabelRepository,
    IDocumentRepository documentRepository,
    ISystemClock clock)
{
    private readonly ITagLabelRepository _tagLabelRepository = tagLabelRepository;
    private readonly IDocumentRepository _documentRepository = documentRepository;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<bool>> HandleAsync(AssignTagToDocumentCommand command, CancellationToken cancellationToken = default)
    {
        if (command.DocumentId == Guid.Empty)
        {
            return OperationResult<bool>.Failure("Document identifier is required.");
        }

        if (command.TagId == Guid.Empty)
        {
            return OperationResult<bool>.Failure("Tag identifier is required.");
        }

        var documentId = DocumentId.FromGuid(command.DocumentId);
        var document = await _documentRepository.GetAsync(documentId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return OperationResult<bool>.Failure("Document was not found.");
        }

        var tagLabel = await _tagLabelRepository.GetByIdAsync(command.TagId, cancellationToken).ConfigureAwait(false);
        if (tagLabel is null)
        {
            return OperationResult<bool>.Failure("Tag label was not found.");
        }

        if (!tagLabel.IsActive)
        {
            return OperationResult<bool>.Failure("Tag label is not active and cannot be assigned.");
        }

        try
        {
            var appliedBy = command.AppliedBy ?? document.CreatedBy;
            document.AssignTag(command.TagId, appliedBy, _clock.UtcNow);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return OperationResult<bool>.Failure(exception.Message);
        }

        await _documentRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return OperationResult<bool>.Success(true);
    }
}
