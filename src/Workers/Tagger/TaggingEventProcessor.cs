using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecm.Rules.Abstractions;
using Microsoft.Extensions.Logging;

namespace Tagger;

internal sealed class TaggingEventProcessor(
    IRuleEngine ruleEngine,
    IDocumentTagAssignmentService assignmentService,
    ITaggingRuleContextFactory contextFactory,
    ILogger<TaggingEventProcessor> logger)
{
    private readonly IDocumentTagAssignmentService _assignmentService = assignmentService
        ?? throw new ArgumentNullException(nameof(assignmentService));
    private readonly ITaggingRuleContextFactory _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));
    private readonly ILogger<TaggingEventProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IRuleEngine _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));

    public Task HandleDocumentUploadedAsync(DocumentUploadedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return EvaluateAsync(integrationEvent, TaggingRuleSetNames.DocumentUploaded, cancellationToken);
    }

    public Task HandleOcrCompletedAsync(OcrCompletedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return EvaluateAsync(integrationEvent, TaggingRuleSetNames.OcrCompleted, cancellationToken);
    }

    private async Task EvaluateAsync(ITaggingIntegrationEvent integrationEvent, string ruleSetName, CancellationToken cancellationToken)
    {
        var context = _contextFactory.Create(integrationEvent);

        var result = _ruleEngine.Execute(ruleSetName, context);
        var matchingTags = ExtractTagIds(result.Output);
        var automaticTags = AutomaticTagProvider.GetAutomaticTags(integrationEvent);

        if (matchingTags.Count == 0 && automaticTags.Count == 0)
        {
            _logger.LogDebug(
                "No tagging rules matched document {DocumentId} for ruleset {RuleSet} and no automatic tags derived.",
                integrationEvent.DocumentId,
                ruleSetName);
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
            "Applied {TagCount} tags to document {DocumentId} via ruleset {RuleSet}.",
            appliedCount,
            integrationEvent.DocumentId,
            ruleSetName);
    }

    private static IReadOnlyCollection<Guid> ExtractTagIds(IReadOnlyDictionary<string, object> output)
    {
        if (!output.TryGetValue("TagIds", out var value) || value is null)
        {
            return Array.Empty<Guid>();
        }

        if (value is IEnumerable<Guid> guidList)
        {
            return guidList.Where(id => id != Guid.Empty).Distinct().ToArray();
        }

        if (value is IEnumerable<object> objects)
        {
            return objects
                .Select(obj => Guid.TryParse(obj?.ToString(), out var parsed) ? parsed : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();
        }

        return Array.Empty<Guid>();
    }
}
