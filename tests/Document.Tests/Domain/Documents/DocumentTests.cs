using ECM.Document.Domain.Documents;
using Xunit;

namespace Document.Tests.Domain.Documents;

using DocumentAggregate = ECM.Document.Domain.Documents.Document;

public class DocumentTests
{
    [Fact]
    public void Create_WhenOptionalFieldsMissing_UsesDefaultsAndTrims()
    {
        var now = DateTimeOffset.UtcNow;
        var ownerId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var title = DocumentTitle.Create(" Document Handbook ");

        var document = DocumentAggregate.Create(
            title,
            "  Policy  ",
            "  Draft  ",
            ownerId,
            createdBy,
            now,
            "   ");

        Assert.Equal(title, document.Title);
        Assert.Equal("Policy", document.DocType);
        Assert.Equal("Draft", document.Status);
        Assert.Equal("Internal", document.Sensitivity);
        Assert.Equal(ownerId, document.OwnerId);
        Assert.Equal(createdBy, document.CreatedBy);
        Assert.Null(document.Department);
        Assert.Equal(now, document.CreatedAtUtc);
        Assert.Equal(now, document.UpdatedAtUtc);
        Assert.Null(document.TypeId);
    }

    [Fact]
    public void Create_WithProvidedSensitivity_TrimsValue()
    {
        var now = DateTimeOffset.UtcNow;
        var document = DocumentAggregate.Create(
            DocumentTitle.Create("Procedure"),
            "Guide",
            "Published",
            Guid.NewGuid(),
            Guid.NewGuid(),
            now,
            department: " Operations ",
            sensitivity: "  Confidential  ");

        Assert.Equal("Confidential", document.Sensitivity);
        Assert.Equal("Operations", document.Department);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingDocType_ThrowsArgumentException(string? docType)
    {
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() => DocumentAggregate.Create(
            DocumentTitle.Create("Doc"),
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

        Assert.Throws<ArgumentException>(() => DocumentAggregate.Create(
            DocumentTitle.Create("Doc"),
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
        var document = DocumentAggregate.Create(
            DocumentTitle.Create("Doc"),
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
        var document = DocumentAggregate.Create(
            DocumentTitle.Create("Doc"),
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
        var document = DocumentAggregate.Create(
            DocumentTitle.Create("Doc"),
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
        var document = DocumentAggregate.Create(
            DocumentTitle.Create("Doc"),
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(() => document.UpdateSensitivity(sensitivity!, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void UpdateDepartment_WithWhitespace_SetsDepartmentToNull()
    {
        var document = DocumentAggregate.Create(
            DocumentTitle.Create("Doc"),
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            department: "Finance");

        var updatedAt = DateTimeOffset.UtcNow.AddMinutes(10);
        document.UpdateDepartment("   ", updatedAt);

        Assert.Null(document.Department);
        Assert.Equal(updatedAt, document.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateDepartment_WithValue_TrimsWhitespace()
    {
        var document = DocumentAggregate.Create(
            DocumentTitle.Create("Doc"),
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        var updatedAt = DateTimeOffset.UtcNow.AddMinutes(15);
        document.UpdateDepartment("  Legal  ", updatedAt);

        Assert.Equal("Legal", document.Department);
        Assert.Equal(updatedAt, document.UpdatedAtUtc);
    }

    [Fact]
    public void AssignTag_WithNewTag_AddsTagAndUpdatesTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var document = DocumentAggregate.Create(
            DocumentTitle.Create("Doc"),
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
        var document = DocumentAggregate.Create(
            DocumentTitle.Create("Doc"),
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
    public void RemoveTag_WithAssignedTag_RemovesAndUpdatesTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var document = DocumentAggregate.Create(
            DocumentTitle.Create("Doc"),
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
        var document = DocumentAggregate.Create(
            DocumentTitle.Create("Doc"),
            "Policy",
            "Draft",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        var removed = document.RemoveTag(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.False(removed);
        Assert.Empty(document.Tags);
    }
}
