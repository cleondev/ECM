using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.Ocr.Application.Commands;
using ECM.Ocr.Application.Models;
using Microsoft.Extensions.Logging;
using Ocr.Tests.TestDoubles;
using Xunit;

namespace Ocr.Tests.Application.Commands;

public sealed class StartOcrCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenProviderReturnsSampleId_LogsWithSampleIdAndReturnsResult()
    {
        var provider = new FakeOcrProvider
        {
            StartResult = new StartOcrResult("sample-123")
        };
        var logger = new FakeLogger<StartOcrCommandHandler>();
        var handler = new StartOcrCommandHandler(provider, logger);
        var command = new StartOcrCommand(
            Guid.NewGuid(),
            "Title",
            "Summary",
            "Content",
            new Dictionary<string, string> { ["key"] = "value" },
            ["tag-1", "tag-2"],
            new Uri("https://files.local/documents/123")
        );

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(provider.CapturedStartCommand, command);
        Assert.Equal(CancellationToken.None, provider.CapturedStartCancellationToken);
        Assert.Equal("sample-123", result.SampleId);

        var logEntry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, logEntry.Level);
        Assert.Contains("sample-123", logEntry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_WhenProviderReturnsEmptySampleId_LogsWithoutSampleId()
    {
        var provider = new FakeOcrProvider
        {
            StartResult = StartOcrResult.Empty
        };
        var logger = new FakeLogger<StartOcrCommandHandler>();
        var handler = new StartOcrCommandHandler(provider, logger);
        var command = new StartOcrCommand(
            Guid.NewGuid(),
            "Document Title",
            null,
            null,
            null,
            null,
            new Uri("https://files.local/documents/456"));

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(StartOcrResult.Empty, result);

        var logEntry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, logEntry.Level);
        Assert.DoesNotContain("sample", logEntry.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(command.DocumentId.ToString(), logEntry.Message, StringComparison.Ordinal);
    }
}
