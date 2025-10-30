using System;
using ECM.Document.Domain.Documents;

namespace ECM.Document.Infrastructure.Persistence.ReadModels;

public sealed class EffectiveAclFlatEntry
{
    public DocumentId DocumentId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset? ValidToUtc { get; set; }

    public bool IsValid { get; set; }

    public string Source { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
