using System;
using System.Collections.Generic;

namespace Tagger;

internal interface ITaggingIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAtUtc { get; }

    Guid DocumentId { get; }

    string Title { get; }

    string? Summary { get; }

    string? Content { get; }

    IDictionary<string, string>? Metadata { get; }

    IReadOnlyCollection<string>? Tags { get; }
}
