using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Tagger;

namespace Tagger.Tests;

internal sealed class TestOptionsMonitor : IOptionsMonitor<TaggingRulesOptions>
{
    private TaggingRulesOptions _value;

    public TestOptionsMonitor(TaggingRulesOptions value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public TaggingRulesOptions CurrentValue => _value;

    public TaggingRulesOptions Get(string? name) => _value;

    public IDisposable OnChange(Action<TaggingRulesOptions, string?> listener) => NullDisposable.Instance;

    public void Set(TaggingRulesOptions value)
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

internal sealed class RecordingRuleEngine : ITaggingRuleEngine
{
    private readonly IReadOnlyCollection<Guid> _result;

    public RecordingRuleEngine(IReadOnlyCollection<Guid>? result = null)
    {
        _result = result ?? Array.Empty<Guid>();
    }

    public TaggingRuleContext? LastContext { get; private set; }

    public TaggingRuleTrigger? LastTrigger { get; private set; }

    public IReadOnlyCollection<Guid> Evaluate(TaggingRuleContext context, TaggingRuleTrigger trigger)
    {
        LastContext = context ?? throw new ArgumentNullException(nameof(context));
        LastTrigger = trigger;
        return _result;
    }
}

internal sealed class RecordingAssignmentService : IDocumentTagAssignmentService
{
    public Guid? LastDocumentId { get; private set; }

    public IReadOnlyCollection<Guid>? LastTagIds { get; private set; }

    public int InvocationCount { get; private set; }

    public int AssignTagsResult { get; set; } = 1;

    public Task<int> AssignTagsAsync(Guid documentId, IReadOnlyCollection<Guid> tagIds, CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        LastDocumentId = documentId;
        LastTagIds = tagIds;
        return Task.FromResult(AssignTagsResult);
    }

    public Task<int> AssignTagsAsync(Guid documentId, IReadOnlyCollection<Guid> tagIds, IReadOnlyCollection<string> tagNames, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
