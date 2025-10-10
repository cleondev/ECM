using ECM.Document.Domain.Documents;
using ECM.Document.Domain.Versions;
using Xunit;

namespace Document.Tests.Domain.Versions;

public class DocumentVersionTests
{
    [Fact]
    public void Constructor_WithValidArguments_TrimsAndAssignsDefaults()
    {
        var documentId = DocumentId.New();
        var createdAt = DateTimeOffset.UtcNow;

        var version = new DocumentVersion(
            Guid.Empty,
            documentId,
            1,
            "  storage/key  ",
            1024,
            "  application/pdf  ",
            "  HASH  ",
            Guid.NewGuid(),
            createdAt);

        Assert.NotEqual(Guid.Empty, version.Id);
        Assert.Equal(documentId, version.DocumentId);
        Assert.Equal(1, version.VersionNo);
        Assert.Equal("storage/key", version.StorageKey);
        Assert.Equal(1024, version.Bytes);
        Assert.Equal("application/pdf", version.MimeType);
        Assert.Equal("HASH", version.Sha256);
        Assert.Equal(createdAt, version.CreatedAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveVersionNo_Throws(int versionNo)
    {
        var documentId = DocumentId.New();

        Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentVersion(
            Guid.NewGuid(),
            documentId,
            versionNo,
            "storage",
            1,
            "application/pdf",
            "hash",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingStorageKey_ThrowsArgumentException(string? storageKey)
    {
        var documentId = DocumentId.New();

        Assert.Throws<ArgumentException>(() => new DocumentVersion(
            Guid.NewGuid(),
            documentId,
            1,
            storageKey!,
            1,
            "application/pdf",
            "hash",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingMimeType_ThrowsArgumentException(string? mimeType)
    {
        var documentId = DocumentId.New();

        Assert.Throws<ArgumentException>(() => new DocumentVersion(
            Guid.NewGuid(),
            documentId,
            1,
            "storage",
            1,
            mimeType!,
            "hash",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingSha256_ThrowsArgumentException(string? sha256)
    {
        var documentId = DocumentId.New();

        Assert.Throws<ArgumentException>(() => new DocumentVersion(
            Guid.NewGuid(),
            documentId,
            1,
            "storage",
            1,
            "application/pdf",
            sha256!,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow));
    }
}
