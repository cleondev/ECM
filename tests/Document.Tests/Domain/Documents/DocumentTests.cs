using System.Text.Json;
using ECM.Document.Domain.Documents;
using DomainDocument = ECM.Document.Domain.Documents.Document;
using Xunit;

namespace Document.Tests.Domain.Documents;

public class DocumentTests
{
    [Fact]
    public void Create_WhenOptionalFieldsMissing_UsesDefaultsAndTrims()
    {
        var now = DateTimeOffset.UtcNow;
        var ownerId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        var document = DomainDocument.Create(
            " Document Handbook ",
            "  Policy  ",
            "  Draft  ",
            ownerId,
            createdBy,
            now);

        Assert.Equal("Document Handbook", document.Title);
        Assert.Equal("Policy", document.DocType);
        Assert.Equal("Draft", document.Status);
        Assert.Equal("Internal", document.Sensitivity);
        Assert.Equal(ownerId, document.OwnerId);
        Assert.Equal(createdBy, document.CreatedBy);
        Assert.Null(document.GroupId);
        Assert.Equal(now, document.CreatedAtUtc);
        Assert.Equal(now, document.UpdatedAtUtc);
        Assert.Null(document.TypeId);
    }

    [Fact]
    public void Create_WithProvidedSensitivityAndGroupId_AssignsValues()
    {
        var now = DateTimeOffset.UtcNow;
        var groupId = Guid.NewGuid();

        var document = DomainDocument.Create(
            "Procedure",
            "Guide",
            "Published",
            Guid.NewGuid(),
            Guid.NewGuid(),
            now,
            groupId: groupId,
            sensitivity: "  Confidential  ");

        Assert.Equal("Confidential", document.Sensitivity);
        Assert.Equal(groupId, document.GroupId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingTitle_ThrowsArgumentException(string? title)
    {
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() => DomainDocument.Create(
            title,
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingDocType_ThrowsArgumentException(string? docType)
    {
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() => DomainDocument.Create(
            "Doc",
            docType!,
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingStatus_ThrowsArgumentException(string? status)
    {
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() => DomainDocument.Create(
            "Doc",
            "Policy",
            status!,
            Guid.NewGuid(),
            Guid.NewGuid(),
            now));
    }

    [Fact]
    public void UpdateStatus_WithValidValue_UpdatesProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var document = DomainDocument.Create(
            "Doc",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            now);

        var updatedAt = now.AddMinutes(5);
        document.UpdateStatus("  Published  ", updatedAt);

        Assert.Equal("Published", document.Status);
        Assert.Equal(updatedAt, document.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateStatus_WithMissingValue_ThrowsArgumentException(string? status)
    {
        var document = DomainDocument.Create(
            "Doc",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() => document.UpdateStatus(status!, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void UpdateSensitivity_WithValidValue_UpdatesProperties()
    {
        var document = DomainDocument.Create(
            "Doc",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        var updatedAt = DateTimeOffset.UtcNow.AddMinutes(2);
        document.UpdateSensitivity("  Restricted  ", updatedAt);

        Assert.Equal("Restricted", document.Sensitivity);
        Assert.Equal(updatedAt, document.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateSensitivity_WithMissingValue_ThrowsArgumentException(string? sensitivity)
    {
        var document = DomainDocument.Create(
            "Doc",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() => document.UpdateSensitivity(sensitivity!, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void UpdateGroupId_WithEmptyGuid_SetsGroupIdToNull()
    {
        var document = DomainDocument.Create(
            "Doc",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            groupId: Guid.NewGuid());

        var updatedAt = DateTimeOffset.UtcNow.AddMinutes(10);
        document.UpdateGroupId(Guid.Empty, updatedAt);

        Assert.Null(document.GroupId);
        Assert.Equal(updatedAt, document.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateGroupId_WithValue_SetsProperty()
    {
        var document = DomainDocument.Create(
            "Doc",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        var updatedAt = DateTimeOffset.UtcNow.AddMinutes(15);
        var groupId = Guid.NewGuid();
        document.UpdateGroupId(groupId, updatedAt);

        Assert.Equal(groupId, document.GroupId);
        Assert.Equal(updatedAt, document.UpdatedAtUtc);
    }

    [Fact]
    public void AssignTag_WithNewTag_AddsTagAndUpdatesTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var document = DomainDocument.Create(
            "Doc",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            now);

        var tagId = Guid.NewGuid();
        var appliedAt = now.AddMinutes(1);

        var documentTag = document.AssignTag(tagId, Guid.NewGuid(), appliedAt);

        Assert.Single(document.Tags);
        Assert.Equal(tagId, documentTag.TagId);
        Assert.Equal(appliedAt, documentTag.AppliedAtUtc);
        Assert.Equal(appliedAt, document.UpdatedAtUtc);
    }

    [Fact]
    public void AssignTag_WithExistingTag_ThrowsInvalidOperationException()
    {
        var document = DomainDocument.Create(
            "Doc",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        var tagId = Guid.NewGuid();
        document.AssignTag(tagId, Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => document.AssignTag(tagId, Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RemoveTag_WithExistingAssignment_RemovesTagAndUpdatesTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var document = DomainDocument.Create(
            "Doc",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            now);

        var tagId = Guid.NewGuid();
        document.AssignTag(tagId, Guid.NewGuid(), now.AddMinutes(1));

        var removedAt = now.AddMinutes(2);
        var removed = document.RemoveTag(tagId, removedAt);

        Assert.True(removed);
        Assert.Empty(document.Tags);
        Assert.Equal(removedAt, document.UpdatedAtUtc);
    }

    [Fact]
    public void RemoveTag_WithMissingAssignment_ReturnsFalse()
    {
        var document = DomainDocument.Create(
            "Doc",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        var removed = document.RemoveTag(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.False(removed);
        Assert.Empty(document.Tags);
    }

    [Fact]
    public void AddVersion_WithValidData_CreatesVersionAndUpdatesTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var createdBy = Guid.NewGuid();
        var document = DomainDocument.Create(
            "Doc",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            createdBy,
            now);

        var version = document.AddVersion(
            "storage-key",
            1024,
            "application/pdf",
            new string('a', 64),
            createdBy,
            now);

        Assert.Equal(1, version.VersionNo);
        Assert.Equal("storage-key", version.StorageKey);
        Assert.Equal(1024, version.Bytes);
        Assert.Equal("application/pdf", version.MimeType);
        Assert.Equal(new string('a', 64), version.Sha256);
        Assert.Equal(createdBy, version.CreatedBy);
        Assert.Equal(now, document.UpdatedAtUtc);
        Assert.Single(document.Versions);
    }

    [Fact]
    public void AddVersion_WithExistingVersions_IncrementsVersionNumber()
    {
        var now = DateTimeOffset.UtcNow;
        var createdBy = Guid.NewGuid();
        var document = DomainDocument.Create(
            "Doc",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            createdBy,
            now);

        document.AddVersion("v1", 100, "application/pdf", new string('a', 64), createdBy, now);

        var later = now.AddMinutes(1);
        var second = document.AddVersion("v2", 200, "application/pdf", new string('b', 64), createdBy, later);

        Assert.Equal(2, second.VersionNo);
        Assert.Equal(later, document.UpdatedAtUtc);
        Assert.Equal(2, document.Versions.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddVersion_WithNonPositiveBytes_ThrowsArgumentOutOfRangeException(long bytes)
    {
        var document = DomainDocument.Create(
            "Doc",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(() => document.AddVersion("storage", bytes, "application/pdf", new string('a', 64), Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void UpdateTitle_WithNewValue_UpdatesDocumentAndTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var document = DomainDocument.Create(
            "Doc",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            now);

        var updatedAt = now.AddMinutes(3);
        document.UpdateTitle("Updated", updatedAt);

        Assert.Equal("Updated", document.Title);
        Assert.Equal(updatedAt, document.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateTitle_WithMissingValue_ThrowsArgumentException()
    {
        var document = DomainDocument.Create(
            "Doc",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() => document.UpdateTitle("   ", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void AttachMetadata_SetsMetadataAndUpdatesTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var document = DomainDocument.Create(
            "Doc",
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            now);

        var metadata = new DocumentMetadata(document.Id, JsonDocument.Parse("{\"key\":\"value\"}"));
        var updatedAt = now.AddMinutes(5);

        document.AttachMetadata(metadata, updatedAt);

        Assert.Same(metadata, document.Metadata);
        Assert.Equal(updatedAt, document.UpdatedAtUtc);
    }
}
