using ECM.Modules.Document.Domain.Documents;
using ECM.Modules.Document.Domain.Signatures;

namespace ECM.Modules.Document.Domain.Versions;

public sealed class DocumentVersion
{
    private readonly List<SignatureRequest> _signatureRequests = new();

    private DocumentVersion()
    {
        StorageKey = null!;
        MimeType = null!;
        Sha256 = null!;
    }

    public DocumentVersion(
        Guid id,
        DocumentId documentId,
        int versionNo,
        string storageKey,
        long bytes,
        string mimeType,
        string sha256,
        Guid createdBy,
        DateTimeOffset createdAtUtc)
        : this()
    {
        if (versionNo <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(versionNo), versionNo, "Version number must be positive.");
        }

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("Storage key is required.", nameof(storageKey));
        }

        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new ArgumentException("MIME type is required.", nameof(mimeType));
        }

        if (string.IsNullOrWhiteSpace(sha256))
        {
            throw new ArgumentException("SHA-256 hash is required.", nameof(sha256));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        DocumentId = documentId;
        VersionNo = versionNo;
        StorageKey = storageKey.Trim();
        Bytes = bytes;
        MimeType = mimeType.Trim();
        Sha256 = sha256.Trim();
        CreatedBy = createdBy;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public DocumentId DocumentId { get; private set; }

    public Document? Document { get; private set; }

    public int VersionNo { get; private set; }

    public string StorageKey { get; private set; }

    public long Bytes { get; private set; }

    public string MimeType { get; private set; }

    public string Sha256 { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<SignatureRequest> SignatureRequests => _signatureRequests.AsReadOnly();

    internal void AddSignatureRequest(SignatureRequest request)
    {
        _signatureRequests.Add(request);
    }
}
