using System.Text.Json;

namespace ECM.Document.Domain.Signatures;

public sealed class SignatureResult
{
    private SignatureResult()
    {
        Status = null!;
        RawResponse = null!;
    }

    public SignatureResult(
        Guid requestId,
        string status,
        string? evidenceHash,
        string? evidenceUrl,
        DateTimeOffset receivedAtUtc,
        JsonDocument rawResponse)
        : this()
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Result status is required.", nameof(status));
        }

        RequestId = requestId;
        Status = status.Trim();
        EvidenceHash = string.IsNullOrWhiteSpace(evidenceHash) ? null : evidenceHash.Trim();
        EvidenceUrl = string.IsNullOrWhiteSpace(evidenceUrl) ? null : evidenceUrl.Trim();
        ReceivedAtUtc = receivedAtUtc;
        RawResponse = rawResponse;
    }

    public Guid RequestId { get; private set; }

    public SignatureRequest? Request { get; private set; }

    public string Status { get; private set; }

    public string? EvidenceHash { get; private set; }

    public string? EvidenceUrl { get; private set; }

    public DateTimeOffset ReceivedAtUtc { get; private set; }

    public JsonDocument RawResponse { get; private set; }
}
