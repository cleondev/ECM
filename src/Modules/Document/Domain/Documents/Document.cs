using System;
using System.Collections.Generic;
using System.Linq;
using ECM.BuildingBlocks.Domain.Events;
using ECM.Document.Domain.DocumentTypes;
using ECM.Document.Domain.Signatures;
using ECM.Document.Domain.Tags;
using ECM.Document.Domain.Versions;
using ECM.Document.Domain.Documents.Events;

namespace ECM.Document.Domain.Documents;

public sealed class Document : IHasDomainEvents
{
    private readonly List<DocumentVersion> _versions = [];
    private readonly List<DocumentTag> _tags = [];
    private readonly List<SignatureRequest> _signatureRequests = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    private Document()
    {
        Title = null!;
        DocType = null!;
        Status = null!;
        Sensitivity = "Internal";
    }

    private Document(
        DocumentId id,
        DocumentTitle title,
        string docType,
        string status,
        string sensitivity,
        Guid ownerId,
        Guid createdBy,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        string? department,
        Guid? typeId)
        : this()
    {
        Id = id;
        Title = title;
        DocType = docType;
        Status = status;
        Sensitivity = sensitivity;
        OwnerId = ownerId;
        CreatedBy = createdBy;
        Department = department;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        TypeId = typeId;
    }

    public DocumentId Id { get; private set; }

    public DocumentTitle Title { get; private set; }

    public string DocType { get; private set; }

    public string Status { get; private set; }

    public string Sensitivity { get; private set; }

    public Guid OwnerId { get; private set; }

    public string? Department { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Guid? TypeId { get; private set; }

    public DocumentType? Type { get; private set; }

    public DocumentMetadata? Metadata { get; private set; }

    public IReadOnlyCollection<DocumentVersion> Versions => _versions.AsReadOnly();

    public IReadOnlyCollection<DocumentTag> Tags => _tags.AsReadOnly();

    public IReadOnlyCollection<SignatureRequest> SignatureRequests => _signatureRequests.AsReadOnly();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Document Create(
        DocumentTitle title,
        string docType,
        string status,
        Guid ownerId,
        Guid createdBy,
        DateTimeOffset now,
        string? department = null,
        string? sensitivity = null,
        Guid? typeId = null)
    {
        if (string.IsNullOrWhiteSpace(docType))
        {
            throw new ArgumentException("Document type is required.", nameof(docType));
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Document status is required.", nameof(status));
        }

        var finalSensitivity = string.IsNullOrWhiteSpace(sensitivity) ? "Internal" : sensitivity.Trim();

        var document = new Document(
            DocumentId.New(),
            title,
            docType.Trim(),
            status.Trim(),
            finalSensitivity,
            ownerId,
            createdBy,
            now,
            now,
            string.IsNullOrWhiteSpace(department) ? null : department.Trim(),
            typeId);

        document.Raise(new DocumentCreatedDomainEvent(
            document.Id,
            document.Title.Value,
            document.OwnerId,
            document.CreatedBy,
            document.CreatedAtUtc));

        return document;
    }

    public void UpdateTitle(DocumentTitle title, DateTimeOffset updatedAtUtc)
    {
        Title = title;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void UpdateStatus(string status, DateTimeOffset updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Document status is required.", nameof(status));
        }

        Status = status.Trim();
        UpdatedAtUtc = updatedAtUtc;
    }

    public void UpdateSensitivity(string sensitivity, DateTimeOffset updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(sensitivity))
        {
            throw new ArgumentException("Document sensitivity is required.", nameof(sensitivity));
        }

        Sensitivity = sensitivity.Trim();
        UpdatedAtUtc = updatedAtUtc;
    }

    public void UpdateDepartment(string? department, DateTimeOffset updatedAtUtc)
    {
        Department = string.IsNullOrWhiteSpace(department) ? null : department.Trim();
        UpdatedAtUtc = updatedAtUtc;
    }

    public void AttachMetadata(DocumentMetadata metadata, DateTimeOffset updatedAtUtc)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        UpdatedAtUtc = updatedAtUtc;
    }

    public DocumentVersion AddVersion(
        string storageKey,
        long bytes,
        string mimeType,
        string sha256,
        Guid createdBy,
        DateTimeOffset createdAtUtc)
    {
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

        if (bytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), bytes, "File size must be greater than zero.");
        }

        var nextVersionNumber = _versions.Count == 0
            ? 1
            : _versions.Max(version => version.VersionNo) + 1;

        var version = new DocumentVersion(
            Guid.NewGuid(),
            Id,
            nextVersionNumber,
            storageKey,
            bytes,
            mimeType,
            sha256,
            createdBy,
            createdAtUtc);

        _versions.Add(version);
        UpdatedAtUtc = createdAtUtc;
        return version;
    }

    internal void AddVersion(DocumentVersion version)
    {
        _versions.Add(version);
    }

    public DocumentTag AssignTag(Guid tagId, Guid? appliedBy, DateTimeOffset appliedAtUtc)
    {
        if (tagId == Guid.Empty)
        {
            throw new ArgumentException("Tag identifier is required.", nameof(tagId));
        }

        if (_tags.Any(tag => tag.TagId == tagId))
        {
            throw new InvalidOperationException("Tag is already assigned to this document.");
        }

        var documentTag = new DocumentTag(Id, tagId, appliedBy, appliedAtUtc);
        _tags.Add(documentTag);
        UpdatedAtUtc = appliedAtUtc;
        return documentTag;
    }

    public bool RemoveTag(Guid tagId, DateTimeOffset removedAtUtc)
    {
        if (tagId == Guid.Empty)
        {
            throw new ArgumentException("Tag identifier is required.", nameof(tagId));
        }

        var existingTag = _tags.FirstOrDefault(tag => tag.TagId == tagId);
        if (existingTag is null)
        {
            return false;
        }

        _tags.Remove(existingTag);
        UpdatedAtUtc = removedAtUtc;
        return true;
    }

    internal void AddSignatureRequest(SignatureRequest request)
    {
        _signatureRequests.Add(request);
    }

    private void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
