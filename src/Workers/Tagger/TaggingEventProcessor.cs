using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Tagger;

internal sealed class TaggingEventProcessor(
    ITaggingRuleEngine ruleEngine,
    IDocumentTagAssignmentService assignmentService,
    ITaggingRuleContextFactory contextFactory,
    ILogger<TaggingEventProcessor> logger)
{
    private readonly IDocumentTagAssignmentService _assignmentService = assignmentService
        ?? throw new ArgumentNullException(nameof(assignmentService));
    private readonly ITaggingRuleContextFactory _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));
    private readonly ILogger<TaggingEventProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ITaggingRuleEngine _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));

    public Task HandleDocumentUploadedAsync(DocumentUploadedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return EvaluateAsync(integrationEvent, TaggingRuleTrigger.DocumentUploaded, cancellationToken);
    }

    public Task HandleOcrCompletedAsync(OcrCompletedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return EvaluateAsync(integrationEvent, TaggingRuleTrigger.OcrCompleted, cancellationToken);
    }

    private async Task EvaluateAsync(ITaggingIntegrationEvent integrationEvent, TaggingRuleTrigger trigger, CancellationToken cancellationToken)
    {
        var context = _contextFactory.Create(integrationEvent);

        var matchingTags = _ruleEngine.Evaluate(context, trigger);
        var automaticTags = AutomaticTagProvider.GetAutomaticTags(integrationEvent);

        if (matchingTags.Count == 0 && automaticTags.Count == 0)
        {
            _logger.LogDebug(
                "No tagging rules matched document {DocumentId} on trigger {Trigger} and no automatic tags derived.",
                integrationEvent.DocumentId,
                trigger);
            return;
        }

        var appliedCount = await _assignmentService
            .AssignTagsAsync(integrationEvent.DocumentId, matchingTags, automaticTags, cancellationToken)
            .ConfigureAwait(false);

        if (appliedCount == 0)
        {
            _logger.LogInformation(
                "Tagging rules matched for document {DocumentId} but no assignments were applied.",
                integrationEvent.DocumentId);
            return;
        }

        _logger.LogInformation(
            "Applied {TagCount} tags to document {DocumentId} via {Trigger} trigger.",
            appliedCount,
            integrationEvent.DocumentId,
            trigger);
    }
}
