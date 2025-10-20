using System;

namespace ECM.Operations.Infrastructure.Persistence;

public sealed class WebhookDelivery
{
    private WebhookDelivery()
    {
        EventType = string.Empty;
        Payload = string.Empty;
        Status = string.Empty;
        Error = string.Empty;
    }

    public WebhookDelivery(Guid webhookId, string eventType, string payload, string status, DateTimeOffset enqueuedAtUtc)
    {
        WebhookId = webhookId;
        EventType = eventType;
        Payload = payload;
        Status = status;
        EnqueuedAtUtc = enqueuedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid WebhookId { get; private set; }

    public string EventType { get; private set; }

    public string Payload { get; private set; }

    public string Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset EnqueuedAtUtc { get; private set; }

    public DateTimeOffset? DeliveredAtUtc { get; private set; }

    public string Error { get; private set; }

    public WebhookSubscription? Webhook { get; private set; }
}
