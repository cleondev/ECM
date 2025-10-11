using System.Text.RegularExpressions;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Domain.Documents;
using ECM.Document.Domain.Tags;

namespace ECM.Document.Application.Tags;

public sealed class TagApplicationService(
    ITagLabelRepository tagLabelRepository,
    IDocumentRepository documentRepository,
    ISystemClock clock)
{
    private static readonly Regex SlugPattern = new("^[a-z0-9_]+(-[a-z0-9_]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ITagLabelRepository _tagLabelRepository = tagLabelRepository;
    private readonly IDocumentRepository _documentRepository = documentRepository;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<TagLabelSummary>> CreateTagAsync(CreateTagLabelCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.NamespaceSlug))
        {
            return OperationResult<TagLabelSummary>.Failure("Namespace slug is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Slug))
        {
            return OperationResult<TagLabelSummary>.Failure("Tag slug is required.");
        }

        var namespaceSlug = command.NamespaceSlug.Trim().ToLowerInvariant();
        var slug = command.Slug.Trim().ToLowerInvariant();
        var path = string.IsNullOrWhiteSpace(command.Path)
            ? slug
            : command.Path.Trim().ToLowerInvariant();

        if (!SlugPattern.IsMatch(slug))
        {
            return OperationResult<TagLabelSummary>.Failure("Slug must contain lowercase letters, numbers, underscores or hyphens.");
        }

        if (!SlugPattern.IsMatch(path))
        {
            return OperationResult<TagLabelSummary>.Failure("Path must contain lowercase letters, numbers, underscores or hyphens.");
        }

        if (!await _tagLabelRepository.NamespaceExistsAsync(namespaceSlug, cancellationToken).ConfigureAwait(false))
        {
            return OperationResult<TagLabelSummary>.Failure("Tag namespace does not exist.");
        }

        var existingTag = await _tagLabelRepository
            .GetByNamespaceAndPathAsync(namespaceSlug, path, cancellationToken)
            .ConfigureAwait(false);

        if (existingTag is not null)
        {
            return OperationResult<TagLabelSummary>.Failure("Tag with the provided namespace and path already exists.");
        }

        var createdAt = _clock.UtcNow;
        var tagLabel = TagLabel.Create(namespaceSlug, slug, path, command.CreatedBy, createdAt);
        await _tagLabelRepository.AddAsync(tagLabel, cancellationToken).ConfigureAwait(false);

        var summary = new TagLabelSummary(
            tagLabel.Id,
            tagLabel.NamespaceSlug,
            tagLabel.Slug,
            tagLabel.Path,
            tagLabel.IsActive,
            tagLabel.CreatedBy,
            tagLabel.CreatedAtUtc);

        return OperationResult<TagLabelSummary>.Success(summary);
    }

    public async Task<OperationResult<bool>> DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        if (tagId == Guid.Empty)
        {
            return OperationResult<bool>.Failure("Tag identifier is required.");
        }

        var tagLabel = await _tagLabelRepository.GetByIdAsync(tagId, cancellationToken).ConfigureAwait(false);
        if (tagLabel is null)
        {
            return OperationResult<bool>.Failure("Tag label was not found.");
        }

        tagLabel.MarkDeleted(_clock.UtcNow);
        await _tagLabelRepository.RemoveAsync(tagLabel, cancellationToken).ConfigureAwait(false);
        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> AssignTagAsync(AssignTagToDocumentCommand command, CancellationToken cancellationToken = default)
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
            document.AssignTag(command.TagId, command.AppliedBy, _clock.UtcNow);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return OperationResult<bool>.Failure(exception.Message);
        }

        await _documentRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<bool>> RemoveTagAsync(RemoveTagFromDocumentCommand command, CancellationToken cancellationToken = default)
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

        var removed = false;
        try
        {
            removed = document.RemoveTag(command.TagId, _clock.UtcNow);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<bool>.Failure(exception.Message);
        }

        if (!removed)
        {
            return OperationResult<bool>.Failure("Tag label is not assigned to the document.");
        }

        await _documentRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return OperationResult<bool>.Success(true);
    }
}
