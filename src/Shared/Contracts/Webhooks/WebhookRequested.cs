namespace Shared.Contracts.Webhooks;

public sealed record WebhookRequested
{
    /// <summary>
    /// Logical identifier of the webhook request.
    /// Used as the primary idempotency key.
    /// </summary>
    public string RequestId { get; init; } = default!;

    /// <summary>
    /// The predefined endpoint key (e.g., "UploadCallback_PGB").
    /// This determines which configured webhook endpoint to invoke.
    /// </summary>
    public string EndpointKey { get; init; } = default!;

    /// <summary>
    /// Normalized JSON payload to be sent to the webhook.
    /// </summary>
    public string PayloadJson { get; init; } = default!;

    /// <summary>
    /// Optional correlation identifier used for distributed tracing
    /// across services, logs, and asynchronous operations.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Timestamp (UTC recommended) indicating when this webhook request was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}
