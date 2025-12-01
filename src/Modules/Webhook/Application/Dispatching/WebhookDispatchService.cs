using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Linq;
using ECM.Webhook.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Shared.Contracts.Webhooks;

namespace ECM.Webhook.Application.Dispatching;

public sealed class WebhookDispatchService
{
    private readonly IWebhookDeliveryRepository _repository;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookDispatchService> _logger;
    private readonly WebhookDispatcherOptions _options;
    private readonly IReadOnlyDictionary<string, WebhookEndpointOptions> _endpoints;
    private readonly AsyncPolicy<HttpResponseMessage> _retryPolicy;

    public WebhookDispatchService(
        IWebhookDeliveryRepository repository,
        HttpClient httpClient,
        IOptions<WebhookDispatcherOptions> options,
        ILogger<WebhookDispatchService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));

        _endpoints = (_options.Endpoints ?? Array.Empty<WebhookEndpointOptions>())
            .Where(endpoint => !string.IsNullOrWhiteSpace(endpoint.Key) && !string.IsNullOrWhiteSpace(endpoint.Url))
            .GroupBy(endpoint => endpoint.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => NormalizeEndpoint(group.Last()),
                StringComparer.OrdinalIgnoreCase);

        _retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(response => !response.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                _options.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(_options.InitialBackoff.TotalSeconds, retryAttempt)),
                onRetry: (outcome, timespan, attempt, _) =>
                {
                    _logger.LogWarning(
                        outcome.Exception,
                        "Webhook dispatch attempt {Attempt} failed. Waiting {Delay} before retrying...",
                        attempt,
                        timespan);
                });
    }

    public async Task DispatchAsync(WebhookRequested request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var delivery = await _repository.FindAsync(request.RequestId, request.EndpointKey, cancellationToken).ConfigureAwait(false);

        if (delivery is not null && string.Equals(delivery.Status, "Succeeded", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "Webhook request {RequestId} for endpoint {EndpointKey} already succeeded. Skipping.",
                request.RequestId,
                request.EndpointKey);
            return;
        }

        delivery ??= new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            RequestId = request.RequestId,
            EndpointKey = request.EndpointKey,
            PayloadJson = request.PayloadJson,
            CorrelationId = request.CorrelationId,
            Status = "Pending",
            CreatedAt = request.CreatedAt
        };

        var endpoint = ResolveEndpoint(request.EndpointKey);
        var attempts = 0;
        var lastAttemptAt = DateTimeOffset.MinValue;

        using var response = await _retryPolicy.ExecuteAsync(async ct =>
        {
            attempts++;
            lastAttemptAt = DateTimeOffset.UtcNow;

            using var requestMessage = new HttpRequestMessage(new HttpMethod(endpoint.HttpMethod), endpoint.Url)
            {
                Content = new StringContent(request.PayloadJson, Encoding.UTF8, "application/json")
            };

            return await _httpClient.SendAsync(requestMessage, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);

        delivery.AttemptCount += attempts;
        delivery.LastAttemptAt = lastAttemptAt;

        var failureMessage = response.IsSuccessStatusCode
            ? null
            : $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase ?? response.StatusCode.ToString()})";

        if (response.IsSuccessStatusCode)
        {
            delivery.MarkSucceeded(lastAttemptAt);
            _logger.LogInformation(
                "Webhook request {RequestId} delivered to {EndpointKey} after {Attempts} attempt(s).",
                request.RequestId,
                request.EndpointKey,
                attempts);
        }
        else
        {
            delivery.MarkFailed(lastAttemptAt, failureMessage);
            _logger.LogWarning(
                "Webhook request {RequestId} to {EndpointKey} failed with status code {StatusCode} after {Attempts} attempt(s).",
                request.RequestId,
                request.EndpointKey,
                response.StatusCode,
                attempts);
        }

        if (delivery.AttemptCount == attempts)
        {
            await _repository.AddAsync(delivery, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _repository.UpdateAsync(delivery, cancellationToken).ConfigureAwait(false);
        }
    }

    private WebhookEndpointOptions ResolveEndpoint(string endpointKey)
    {
        if (_endpoints.TryGetValue(endpointKey, out var endpoint))
        {
            return endpoint;
        }

        throw new InvalidOperationException($"No webhook endpoint configured for key '{endpointKey}'.");
    }

    private static WebhookEndpointOptions NormalizeEndpoint(WebhookEndpointOptions endpoint)
    {
        return new WebhookEndpointOptions
        {
            Key = endpoint.Key,
            Url = endpoint.Url,
            HttpMethod = string.IsNullOrWhiteSpace(endpoint.HttpMethod)
                ? HttpMethod.Post.Method
                : endpoint.HttpMethod
        };
    }
}
