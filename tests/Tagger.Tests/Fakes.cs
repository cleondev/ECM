using Ecm.Rules.Abstractions;

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Tagger.Events;
using Tagger.Rules.Configuration;
using Tagger.Services;

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

internal sealed class TestOptionsSnapshot<TOptions> : IOptionsSnapshot<TOptions> where TOptions : class
{
    public TestOptionsSnapshot(TOptions value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public TOptions Value { get; }

    public TOptions Get(string? name) => Value;
}

internal sealed class RecordingRuleEngine : IRuleEngine
{
    private readonly IReadOnlyCollection<Guid> _result;
    private readonly IReadOnlyCollection<string> _tagNames;
    private readonly IReadOnlyCollection<TagDefinition> _tagDefinitions;

    public RecordingRuleEngine(
        IReadOnlyCollection<Guid>? result = null,
        IReadOnlyCollection<TagDefinition>? tagDefinitions = null,
        IReadOnlyCollection<string>? tagNames = null)
    {
        _result = result ?? Array.Empty<Guid>();
        _tagNames = tagNames ?? Array.Empty<string>();
        _tagDefinitions = tagDefinitions ?? Array.Empty<TagDefinition>();
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
                ["TagIds"] = _result,
                ["Tags"] = _tagDefinitions,
                ["TagNames"] = _tagNames
            }
        };
    }
}

internal sealed class RecordingRuleSetSelector : ITaggingRuleSetSelector
{
    public ITaggingIntegrationEvent? LastEvent { get; private set; }

    public IReadOnlyCollection<string> RuleSets { get; set; } = Array.Empty<string>();

    public IReadOnlyCollection<string> GetRuleSets(ITaggingIntegrationEvent integrationEvent)
    {
        LastEvent = integrationEvent;
        return RuleSets;
    }
}

internal sealed class RecordingAssignmentService : IDocumentTagAssignmentService
{
    public Guid? LastDocumentId { get; private set; }

    public IReadOnlyCollection<Guid>? LastTagIds { get; private set; }

    public IReadOnlyCollection<TagDefinition>? LastTagDefinitions { get; private set; }

    public int InvocationCount { get; private set; }

    public int AssignTagsResult { get; set; } = 1;

    public Task<int> AssignTagsAsync(
        Guid documentId,
        IReadOnlyCollection<Guid> tagIds,
        IReadOnlyCollection<TagDefinition> tagDefinitions,
        CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        LastDocumentId = documentId;
        LastTagIds = tagIds;
        LastTagDefinitions = tagDefinitions;
        return Task.FromResult(AssignTagsResult);
    }
}

internal sealed class TestHostEnvironment : IHostEnvironment
{
    public TestHostEnvironment(string contentRoot)
    {
        ContentRootPath = contentRoot;
    }

    public string ApplicationName { get; set; } = "Tagger.Tests";

    public IFileProvider ContentRootFileProvider { get; set; } = null!;

    public string ContentRootPath { get; set; }

    public string EnvironmentName { get; set; } = Environments.Production;
}
