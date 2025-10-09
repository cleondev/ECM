using System;

namespace ECM.Modules.Signature.Domain.Requests;

public sealed class SignatureRequest
{
    public SignatureRequest(Guid id, Guid documentId, string signerEmail, SignatureStatus status, DateTimeOffset requestedAtUtc)
    {
        Id = id;
        DocumentId = documentId;
        SignerEmail = signerEmail;
        Status = status;
        RequestedAtUtc = requestedAtUtc;
    }

    public Guid Id { get; }

    public Guid DocumentId { get; }

    public string SignerEmail { get; }

    public SignatureStatus Status { get; private set; }

    public DateTimeOffset RequestedAtUtc { get; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public void MarkCompleted(DateTimeOffset completedAtUtc)
    {
        Status = SignatureStatus.Completed;
        CompletedAtUtc = completedAtUtc;
    }
}
