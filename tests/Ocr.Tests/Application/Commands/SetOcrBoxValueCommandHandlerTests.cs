using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.Ocr.Application.Commands;
using Microsoft.Extensions.Logging;
using Ocr.Tests.TestDoubles;
using Xunit;

namespace Ocr.Tests.Application.Commands;

public sealed class SetOcrBoxValueCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_DelegatesToProviderAndLogs()
    {
        var provider = new FakeOcrProvider();
        var logger = new FakeLogger<SetOcrBoxValueCommandHandler>();
        var handler = new SetOcrBoxValueCommandHandler(provider, logger);
        var command = new SetOcrBoxValueCommand("sample-1", "box-3", "Accepted");

        await handler.HandleAsync(command, CancellationToken.None);

        var call = Assert.Single(provider.SetBoxValueCalls);
        Assert.Equal(("sample-1", "box-3", "Accepted", CancellationToken.None), call);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("box-3", entry.Message, StringComparison.Ordinal);
        Assert.Contains("sample-1", entry.Message, StringComparison.Ordinal);
    }
}
