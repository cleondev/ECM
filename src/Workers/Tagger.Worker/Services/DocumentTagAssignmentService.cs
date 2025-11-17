using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.Document.Application.Tags.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tagger;

internal interface IDocumentTagAssignmentService
{
    Task<int> AssignTagsAsync(Guid documentId, IReadOnlyCollection<Guid> tagIds, CancellationToken cancellationToken = default);
}

internal sealed class DocumentTagAssignmentService : IDocumentTagAssignmentService
{
    private readonly AssignTagToDocumentCommandHandler _handler;
    private readonly ILogger<DocumentTagAssignmentService> _logger;
    private readonly IOptionsMonitor<TaggingRulesOptions> _options;

    public DocumentTagAssignmentService(
        AssignTagToDocumentCommandHandler handler,
        ILogger<DocumentTagAssignmentService> logger,
        IOptionsMonitor<TaggingRulesOptions> options)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<int> AssignTagsAsync(Guid documentId, IReadOnlyCollection<Guid> tagIds, CancellationToken cancellationToken = default)
    {
        if (tagIds is null || tagIds.Count == 0)
        {
            return 0;
        }

        var appliedBy = _options.CurrentValue.AppliedBy;
        var appliedCount = 0;
        var seen = new HashSet<Guid>();

        foreach (var tagId in tagIds.Where(tagId => tagId != Guid.Empty))
        {
            if (!seen.Add(tagId))
            {
                continue;
            }

            var result = await _handler
                .HandleAsync(new AssignTagToDocumentCommand(documentId, tagId, appliedBy), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure || result.Value is not true)
            {
                if (result.Errors.Count > 0)
                {
                    _logger.LogWarning(
                        "Skipping tag {TagId} for document {DocumentId}: {Reason}",
                        tagId,
                        documentId,
                        string.Join("; ", result.Errors));
                }

                continue;
            }

            appliedCount++;
        }

        return appliedCount;
    }
}
