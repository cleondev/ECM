using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ecm.Rules.Abstractions;
using Microsoft.Extensions.Options;
using Tagger;

namespace Tagger.Tests;

internal sealed class TestOptionsMonitor<TOptions> : IOptionsMonitor<TOptions> where TOptions : class
{
    private TOptions _value;

    public TestOptionsMonitor(TOptions value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public TOptions CurrentValue => _value;

    public TOptions Get(string? name) => _value;

    public IDisposable OnChange(Action<TOptions, string?> listener) => NullDisposable.Instance;

    public void Set(TOptions value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}

internal sealed class RecordingRuleEngine : IRuleEngine
{
    private readonly IReadOnlyCollection<Guid> _result;

    public RecordingRuleEngine(IReadOnlyCollection<Guid>? result = null)
    {
        _result = result ?? Array.Empty<Guid>();
    }

    public IRuleContext? LastContext { get; private set; }

    public string? LastRuleSet { get; private set; }

    public RuleExecutionResult Execute(string ruleSetName, IRuleContext context)
    {
        LastContext = context ?? throw new ArgumentNullException(nameof(context));
        LastRuleSet = ruleSetName;

        return new RuleExecutionResult
        {
            RuleSetName = ruleSetName,
            Output = new Dictionary<string, object>
            {
                ["TagIds"] = _result
            }
        };
    }
}

internal sealed class TestTaggingRuleSource : ITaggingRuleSource
{
    private readonly IReadOnlyCollection<TaggingRuleOptions>? _rules;

    public TestTaggingRuleSource(IReadOnlyCollection<TaggingRuleOptions>? rules)
    {
        _rules = rules;
    }

    public IReadOnlyCollection<TaggingRuleOptions> GetRules() => _rules ?? Array.Empty<TaggingRuleOptions>();
}

internal sealed class RecordingAssignmentService : IDocumentTagAssignmentService
{
    public Guid? LastDocumentId { get; private set; }

    public IReadOnlyCollection<Guid>? LastTagIds { get; private set; }

    public IReadOnlyCollection<string>? LastTagNames { get; private set; }

    public int InvocationCount { get; private set; }

    public int AssignTagsResult { get; set; } = 1;

    public Task<int> AssignTagsAsync(
        Guid documentId,
        IReadOnlyCollection<Guid> tagIds,
        IReadOnlyCollection<string> tagNames,
        CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        LastDocumentId = documentId;
        LastTagIds = tagIds;
        LastTagNames = tagNames;
        return Task.FromResult(AssignTagsResult);
    }
}
