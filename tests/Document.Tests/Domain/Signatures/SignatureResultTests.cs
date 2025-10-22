using System;
using System.Text.Json;
using ECM.Document.Domain.Signatures;
using Xunit;

namespace Document.Tests.Domain.Signatures;

public class SignatureResultTests
{
    [Fact]
    public void Constructor_WithValidValues_SetsProperties()
    {
        var requestId = Guid.NewGuid();
        var receivedAt = DateTimeOffset.UtcNow;
        using var rawResponse = JsonDocument.Parse("{\"status\":\"completed\"}");

        var result = new SignatureResult(
            requestId,
            "  accepted  ",
            "  hash  ",
            "  https://example.com  ",
            receivedAt,
            rawResponse);

        Assert.Equal(requestId, result.RequestId);
        Assert.Equal("accepted", result.Status);
        Assert.Equal("hash", result.EvidenceHash);
        Assert.Equal("https://example.com", result.EvidenceUrl);
        Assert.Equal(receivedAt, result.ReceivedAtUtc);
        Assert.Equal(rawResponse, result.RawResponse);
    }

    [Fact]
    public void Constructor_WithNullEvidence_TrimsToNull()
    {
        var requestId = Guid.NewGuid();
        using var rawResponse = JsonDocument.Parse("{}");

        var result = new SignatureResult(
            requestId,
            "completed",
            "   ",
            null,
            DateTimeOffset.UtcNow,
            rawResponse);

        Assert.Null(result.EvidenceHash);
        Assert.Null(result.EvidenceUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidStatus_Throws(string? status)
    {
        using var rawResponse = JsonDocument.Parse("{}");

        Assert.Throws<ArgumentException>(() => new SignatureResult(
            Guid.NewGuid(),
            status!,
            null,
            null,
            DateTimeOffset.UtcNow,
            rawResponse));
    }
}
