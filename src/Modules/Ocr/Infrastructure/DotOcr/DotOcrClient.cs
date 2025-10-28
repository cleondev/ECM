using System.Net.Http.Json;
using System.Text.Json;
using ECM.Ocr.Application.Abstractions;
using ECM.Ocr.Application.Commands;
using ECM.Ocr.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECM.Ocr.Infrastructure.DotOcr;

internal sealed class DotOcrClient : IOcrProvider
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<DotOcrOptions> _options;
    private readonly ILogger<DotOcrClient> _logger;

    public DotOcrClient(HttpClient httpClient, IOptionsMonitor<DotOcrOptions> options, ILogger<DotOcrClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<StartOcrResult> StartProcessingAsync(StartOcrCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var options = _options.CurrentValue;
        ArgumentNullException.ThrowIfNull(command.FileUrl);

        var payload = new
        {
            model = options.Model,
            temperature = options.Temperature,
            max_tokens = options.MaxTokens,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = command.FileUrl.ToString()
                            }
                        },
                        new
                        {
                            type = "text",
                            text = options.Instruction ?? string.Empty
                        }
                    }
                }
            }
        };

        using var response = await _httpClient.PostAsJsonAsync(options.ChatCompletionsEndpoint, payload, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, "trigger OCR processing", cancellationToken).ConfigureAwait(false);

        if (!HasContent(response))
        {
            return StartOcrResult.Empty;
        }

        var json = await ReadJsonElementAsync(response, cancellationToken).ConfigureAwait(false);
        var sampleId = TryGetSampleId(json);

        return string.IsNullOrWhiteSpace(sampleId)
            ? StartOcrResult.Empty
            : new StartOcrResult(sampleId);
    }

    public async Task<OcrResult> GetSampleResultAsync(string sampleId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sampleId);

        var options = _options.CurrentValue;
        var endpoint = BuildEndpoint(options.SampleResultEndpointTemplate, sampleId: sampleId);

        using var response = await _httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, $"retrieve OCR sample {sampleId}", cancellationToken).ConfigureAwait(false);

        if (!HasContent(response))
        {
            return OcrResult.Empty;
        }

        var json = await ReadJsonElementAsync(response, cancellationToken).ConfigureAwait(false);
        return new OcrResult(json);
    }

    public async Task<OcrResult> GetBoxingResultAsync(string sampleId, string boxingId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sampleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(boxingId);

        var options = _options.CurrentValue;
        var endpoint = BuildEndpoint(options.BoxingResultEndpointTemplate, sampleId: sampleId, boxingId: boxingId);

        using var response = await _httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, $"retrieve OCR boxing {boxingId} for sample {sampleId}", cancellationToken)
            .ConfigureAwait(false);

        if (!HasContent(response))
        {
            return OcrResult.Empty;
        }

        var json = await ReadJsonElementAsync(response, cancellationToken).ConfigureAwait(false);
        return new OcrResult(json);
    }

    public async Task<OcrBoxesResult> ListBoxesAsync(string sampleId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sampleId);

        var options = _options.CurrentValue;
        var endpoint = BuildEndpoint(options.BoxesEndpointTemplate, sampleId: sampleId);

        using var response = await _httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, $"list OCR boxes for sample {sampleId}", cancellationToken).ConfigureAwait(false);

        if (!HasContent(response))
        {
            return OcrBoxesResult.Empty;
        }

        var json = await ReadJsonElementAsync(response, cancellationToken).ConfigureAwait(false);
        return new OcrBoxesResult(json);
    }

    public async Task SetBoxValueAsync(string sampleId, string boxId, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sampleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(boxId);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var options = _options.CurrentValue;
        var endpoint = BuildEndpoint(options.BoxValueEndpointTemplate, sampleId: sampleId, boxId: boxId);
        var payload = new { value };

        using var response = await _httpClient.PutAsJsonAsync(endpoint, payload, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, $"update OCR box {boxId} for sample {sampleId}", cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = response.Content is null
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogError(
            "Dot OCR request to {Operation} failed with status {StatusCode}. Response: {Response}",
            operation,
            (int)response.StatusCode,
            content);

        throw new HttpRequestException($"Failed to {operation}. Status code: {(int)response.StatusCode}. Response: {content}");
    }

    private static bool HasContent(HttpResponseMessage response)
    {
        if (response.Content is null)
        {
            return false;
        }

        if (response.Content.Headers.ContentLength.HasValue)
        {
            return response.Content.Headers.ContentLength.Value > 0;
        }

        return true;
    }

    private static async Task<JsonElement> ReadJsonElementAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return document.RootElement.Clone();
    }

    private static string? TryGetSampleId(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("sampleId", out var sampleId) && sampleId.ValueKind == JsonValueKind.String)
            {
                return sampleId.GetString();
            }

            if (element.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
            {
                return id.GetString();
            }
        }

        return null;
    }

    private static string BuildEndpoint(string template, string? sampleId = null, string? boxingId = null, string? boxId = null)
    {
        var endpoint = template;

        if (!string.IsNullOrEmpty(sampleId))
        {
            endpoint = endpoint.Replace("{sampleId}", Uri.EscapeDataString(sampleId), StringComparison.Ordinal);
        }

        if (!string.IsNullOrEmpty(boxingId))
        {
            endpoint = endpoint.Replace("{boxingId}", Uri.EscapeDataString(boxingId), StringComparison.Ordinal);
        }

        if (!string.IsNullOrEmpty(boxId))
        {
            endpoint = endpoint.Replace("{boxId}", Uri.EscapeDataString(boxId), StringComparison.Ordinal);
        }

        return endpoint;
    }
}
