using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ecm.Rules.Abstractions;
using Microsoft.Extensions.Logging;

using Tagger.Events;
using Tagger.Rules.Configuration;
using Tagger.Services;

namespace Tagger.Processing;

internal sealed class TaggingEventProcessor(
    IRuleEngine ruleEngine,
    IDocumentTagAssignmentService assignmentService,
    ITaggingRuleSetSelector ruleSetSelector,
    ITaggingRuleContextFactory contextFactory,
    ILogger<TaggingEventProcessor> logger)
{
    private readonly IDocumentTagAssignmentService _assignmentService = assignmentService
        ?? throw new ArgumentNullException(nameof(assignmentService));
    private readonly ITaggingRuleContextFactory _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));
    private readonly ILogger<TaggingEventProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ITaggingRuleSetSelector _ruleSetSelector = ruleSetSelector
        ?? throw new ArgumentNullException(nameof(ruleSetSelector));
    private readonly IRuleEngine _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));

    public Task HandleDocumentUploadedAsync(DocumentUploadedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return EvaluateAsync(integrationEvent, cancellationToken);
    }

    public Task HandleOcrCompletedAsync(OcrCompletedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return EvaluateAsync(integrationEvent, cancellationToken);
    }

    private async Task EvaluateAsync(ITaggingIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var context = _contextFactory.Create(integrationEvent);
        var ruleSetNames = _ruleSetSelector.GetRuleSets(integrationEvent);

        if (ruleSetNames.Count == 0)
        {
            _logger.LogDebug(
                "No tagging rulesets mapped for event {EventName}; skipping evaluation for document {DocumentId}.",
                TaggingIntegrationEventNames.FromEvent(integrationEvent),
                integrationEvent.DocumentId);
            return;
        }

        foreach (var ruleSetName in ruleSetNames)
        {
            await EvaluateRuleSetAsync(integrationEvent, ruleSetName, context, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task EvaluateRuleSetAsync(
        ITaggingIntegrationEvent integrationEvent,
        string ruleSetName,
        IRuleContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ruleSetName))
        {
            return;
        }

        var result = _ruleEngine.Execute(ruleSetName, context);
        var matchingTags = ExtractTagIds(result.Output);
        var derivedTagNames = ExtractTagNames(result.Output);

        if (matchingTags.Count == 0 && derivedTagNames.Count == 0)
        {
            _logger.LogDebug(
                "No tagging rules matched document {DocumentId} for ruleset {RuleSet} and no tag names were derived.",
                integrationEvent.DocumentId,
                ruleSetName);
            return;
        }

        var appliedCount = await _assignmentService
            .AssignTagsAsync(integrationEvent.DocumentId, matchingTags, derivedTagNames, cancellationToken)
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

    private static IReadOnlyCollection<string> ExtractTagNames(IReadOnlyDictionary<string, object> output)
    {
        if (!output.TryGetValue("TagNames", out var value) || value is null)
        {
            return Array.Empty<string>();
        }

        if (value is IEnumerable<string> names)
        {
            return names
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        if (value is IEnumerable<object> objects)
        {
            return objects
                .Select(obj => obj?.ToString())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return Array.Empty<string>();
    }
}
