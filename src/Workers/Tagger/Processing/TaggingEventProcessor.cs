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

/// <summary>
/// Orchestrates rule evaluation for tagging events and applies any tags produced by matching rules.
/// </summary>
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

    /// <summary>
    /// Handles a document uploaded event by evaluating the configured rulesets for uploads.
    /// </summary>
    public Task HandleDocumentUploadedAsync(DocumentUploadedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return EvaluateAsync(integrationEvent, cancellationToken);
    }

    /// <summary>
    /// Handles an OCR completed event by evaluating the configured rulesets for OCR results.
    /// </summary>
    public Task HandleOcrCompletedAsync(OcrCompletedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return EvaluateAsync(integrationEvent, cancellationToken);
    }

    /// <summary>
    /// Builds the rule context, resolves active rulesets for the event, and evaluates them sequentially.
    /// </summary>
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

    /// <summary>
    /// Executes a single ruleset against the rule context and applies resulting tag IDs or tag definitions.
    /// </summary>
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
        var derivedTags = ExtractTagDefinitions(result.Output);

        if (matchingTags.Count == 0 && derivedTags.Count == 0)
        {
            _logger.LogDebug(
                "No tagging rules matched document {DocumentId} for ruleset {RuleSet} and no tag definitions were derived.",
                integrationEvent.DocumentId,
                ruleSetName);
            return;
        }

        var appliedCount = await _assignmentService
            .AssignTagsAsync(integrationEvent.DocumentId, matchingTags, derivedTags, cancellationToken)
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

    private static IReadOnlyCollection<TagDefinition> ExtractTagDefinitions(IReadOnlyDictionary<string, object> output)
    {
        var tags = new List<TagDefinition>();

        if (output.TryGetValue("Tags", out var tagValues) && tagValues is not null)
        {
            AddDefinitionsFromValue(tags, tagValues);
        }

        if (output.TryGetValue("TagNames", out var tagNames) && tagNames is not null)
        {
            foreach (var name in ExtractTagNameStrings(tagNames))
            {
                if (TagDefinition.TryCreate(name, out var tag) && tag is not null)
                {
                    tags.Add(tag);
                }
            }
        }

        return tags
            .Where(tag => tag is not null)
            .Distinct(TagDefinition.Comparer)
            .ToArray();
    }

    private static void AddDefinitionsFromValue(ICollection<TagDefinition> definitions, object value)
    {
        switch (value)
        {
            case IEnumerable<TagDefinition> typed:
                foreach (var definition in typed)
                {
                    if (definition is not null)
                    {
                        definitions.Add(definition);
                    }
                }

                break;
            case IEnumerable<object> objects:
                foreach (var entry in objects)
                {
                    if (TagDefinition.TryCreate(entry, out var parsed) && parsed is not null)
                    {
                        definitions.Add(parsed);
                    }
                }

                break;
            default:
                if (TagDefinition.TryCreate(value, out var single) && single is not null)
                {
                    definitions.Add(single);
                }

                break;
        }
    }

    private static IEnumerable<string> ExtractTagNameStrings(object value)
    {
        if (value is IEnumerable<string> names)
        {
            return names
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim());
        }

        if (value is IEnumerable<object> objects)
        {
            return objects
                .Select(obj => obj?.ToString())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!.Trim());
        }

        return Array.Empty<string>();
    }
}
