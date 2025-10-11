using System.Text.Json;
using ECM.Document.Domain.Documents;
using ECM.Document.Domain.Versions;
using DomainDocument = ECM.Document.Domain.Documents.Document;

namespace ECM.Document.Domain.Signatures;

public sealed class SignatureRequest
{
    private SignatureRequest()
    {
        Provider = null!;
        RequestReference = null!;
        Status = "pending";
        Payload = null!;
    }

    public SignatureRequest(
        Guid id,
        DocumentId documentId,
        Guid versionId,
        string provider,
        string requestReference,
        Guid requestedBy,
        string status,
        JsonDocument payload,
        DateTimeOffset createdAtUtc)
        : this()
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("Provider is required.", nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(requestReference))
        {
            throw new ArgumentException("Request reference is required.", nameof(requestReference));
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Request status is required.", nameof(status));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        DocumentId = documentId;
        VersionId = versionId;
        Provider = provider.Trim();
        RequestReference = requestReference.Trim();
        RequestedBy = requestedBy;
        Status = status.Trim();
        Payload = payload;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public DocumentId DocumentId { get; private set; }

    public Guid VersionId { get; private set; }

    public DomainDocument? Document { get; private set; }

    public DocumentVersion? Version { get; private set; }

    public string Provider { get; private set; }

    public string RequestReference { get; private set; }

    public Guid RequestedBy { get; private set; }

    public string Status { get; private set; }

    public JsonDocument Payload { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public SignatureResult? Result { get; private set; }
}
