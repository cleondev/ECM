using System;

namespace ECM.Signature.Domain.Requests;

public sealed class SignatureRequest(Guid id, Guid documentId, string signerEmail, SignatureStatus status, DateTimeOffset requestedAtUtc)
{
    public Guid Id { get; } = id;

    public Guid DocumentId { get; } = documentId;

    public string SignerEmail { get; } = signerEmail;

    public SignatureStatus Status { get; private set; } = status;

    public DateTimeOffset RequestedAtUtc { get; } = requestedAtUtc;

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public void MarkCompleted(DateTimeOffset completedAtUtc)
    {
        Status = SignatureStatus.Completed;
        CompletedAtUtc = completedAtUtc;
    }

    public void Cancel(DateTimeOffset cancelledAtUtc)
    {
        Status = SignatureStatus.Cancelled;
        CompletedAtUtc = cancelledAtUtc;
    }
}
