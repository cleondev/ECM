using System;

namespace ECM.Operations.Infrastructure.Persistence;

public sealed class RetentionCandidate
{
    private RetentionCandidate()
    {
        Reason = string.Empty;
    }

    public RetentionCandidate(Guid documentId, Guid policyId, DateTimeOffset dueAtUtc, string? reason)
    {
        DocumentId = documentId;
        PolicyId = policyId;
        DueAtUtc = dueAtUtc;
        Reason = reason ?? string.Empty;
    }

    public Guid DocumentId { get; private set; }

    public Guid PolicyId { get; private set; }

    public DateTimeOffset DueAtUtc { get; private set; }

    public string Reason { get; private set; }

    public RetentionPolicy? Policy { get; private set; }
}
