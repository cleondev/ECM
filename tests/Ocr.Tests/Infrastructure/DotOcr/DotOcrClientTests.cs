using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ECM.Ocr.Application.Commands;
using ECM.Ocr.Application.Models;
using ECM.Ocr.Infrastructure.DotOcr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocr.Tests.TestDoubles;
using Xunit;

namespace Ocr.Tests.Infrastructure.DotOcr;

public sealed class DotOcrClientTests
{
    [Fact]
    public async Task StartProcessingAsync_WhenResponseContainsSampleId_ReturnsResult()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { sampleId = "sample-99" })
        };
        var handler = new RecordingHttpMessageHandler(_ => Task.FromResult(response));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://dot-ocr.local/")
        };
        var client = CreateClient(httpClient);
        var command = new StartOcrCommand(Guid.NewGuid(), "Title", null, null, null, null);

        var result = await client.StartProcessingAsync(command, CancellationToken.None);

        Assert.Equal("sample-99", result.SampleId);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(new Uri("https://dot-ocr.local/api/samples"), request.Uri);
        Assert.Contains("\"Title\"", request.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartProcessingAsync_WhenResponseHasNoContent_ReturnsEmpty()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Accepted);
        var handler = new RecordingHttpMessageHandler(_ => Task.FromResult(response));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://dot-ocr.local/")
        };
        var client = CreateClient(httpClient);
        var command = new StartOcrCommand(Guid.NewGuid(), "Title", null, null, null, null);

        var result = await client.StartProcessingAsync(command, CancellationToken.None);

        Assert.Equal(StartOcrResult.Empty, result);
    }

    [Fact]
    public async Task GetSampleResultAsync_WhenSuccessful_ReturnsOcrResult()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { status = "done" })
        };
        var handler = new RecordingHttpMessageHandler(_ => Task.FromResult(response));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://dot-ocr.local/")
        };
        var client = CreateClient(httpClient);

        var result = await client.GetSampleResultAsync("sample 42", CancellationToken.None);

        Assert.Equal("done", result.Data.GetProperty("status").GetString());
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(new Uri("https://dot-ocr.local/api/samples/sample%2042/results"), request.Uri);
    }

    [Fact]
    public async Task SetBoxValueAsync_WhenResponseFails_ThrowsAndLogs()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Unexpected error.")
        };
        var handler = new RecordingHttpMessageHandler(_ => Task.FromResult(response));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://dot-ocr.local/")
        };
        var logger = new FakeLogger<DotOcrClient>();
        var optionsMonitor = new TestOptionsMonitor<DotOcrOptions>(new DotOcrOptions());
        var client = new DotOcrClient(httpClient, optionsMonitor, logger);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.SetBoxValueAsync("sample/1", "box+1", "approved", CancellationToken.None));

        Assert.Contains("Failed to update OCR box", exception.Message, StringComparison.Ordinal);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("update OCR box box+1 for sample sample/1", entry.Message, StringComparison.Ordinal);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(new Uri("https://dot-ocr.local/api/samples/sample%2F1/boxes/box%2B1"), request.Uri);
        Assert.Contains("approved", request.Content, StringComparison.Ordinal);
    }

    private static DotOcrClient CreateClient(HttpClient httpClient)
    {
        var options = new DotOcrOptions();
        var optionsMonitor = new TestOptionsMonitor<DotOcrOptions>(options);
        var logger = new FakeLogger<DotOcrClient>();
        return new DotOcrClient(httpClient, optionsMonitor, logger);
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public List<RequestRecord> Requests { get; } = [];

        public RecordingHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            Requests.Add(new RequestRecord(request.Method, request.RequestUri, content));
            return await _handler(request).ConfigureAwait(false);
        }
    }

    private sealed record RequestRecord(HttpMethod Method, Uri? Uri, string? Content);

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T> where T : class
    {
        public TestOptionsMonitor(T currentValue)
        {
            CurrentValue = currentValue;
        }

        public T CurrentValue { get; private set; }

        public T Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
