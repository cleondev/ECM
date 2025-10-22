using System;
using System.Text.Json;
using ECM.Document.Domain.Documents;
using ECM.Document.Domain.Signatures;
using Xunit;

namespace Document.Tests.Domain.Signatures;

public class SignatureRequestTests
{
    [Fact]
    public void Constructor_WithValidValues_SetsProperties()
    {
        var documentId = DocumentId.New();
        var versionId = Guid.NewGuid();
        var requestedBy = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        using var payload = JsonDocument.Parse("{\"key\":\"value\"}");

        var request = new SignatureRequest(
            Guid.Empty,
            documentId,
            versionId,
            "  adobe-sign  ",
            "  ref-123  ",
            requestedBy,
            "  pending  ",
            payload,
            createdAt);

        Assert.Equal(documentId, request.DocumentId);
        Assert.Equal(versionId, request.VersionId);
        Assert.Equal("adobe-sign", request.Provider);
        Assert.Equal("ref-123", request.RequestReference);
        Assert.Equal(requestedBy, request.RequestedBy);
        Assert.Equal("pending", request.Status);
        Assert.Equal(payload, request.Payload);
        Assert.Equal(createdAt, request.CreatedAtUtc);
        Assert.NotEqual(Guid.Empty, request.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidProvider_Throws(string? provider)
    {
        var documentId = DocumentId.New();
        using var payload = JsonDocument.Parse("{}");

        Assert.Throws<ArgumentException>(() => new SignatureRequest(
            Guid.NewGuid(),
            documentId,
            Guid.NewGuid(),
            provider!,
            "ref",
            Guid.NewGuid(),
            "pending",
            payload,
            DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidRequestReference_Throws(string? reference)
    {
        var documentId = DocumentId.New();
        using var payload = JsonDocument.Parse("{}");

        Assert.Throws<ArgumentException>(() => new SignatureRequest(
            Guid.NewGuid(),
            documentId,
            Guid.NewGuid(),
            "provider",
            reference!,
            Guid.NewGuid(),
            "pending",
            payload,
            DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidStatus_Throws(string? status)
    {
        var documentId = DocumentId.New();
        using var payload = JsonDocument.Parse("{}");

        Assert.Throws<ArgumentException>(() => new SignatureRequest(
            Guid.NewGuid(),
            documentId,
            Guid.NewGuid(),
            "provider",
            "ref",
            Guid.NewGuid(),
            status!,
            payload,
            DateTimeOffset.UtcNow));
    }
}
