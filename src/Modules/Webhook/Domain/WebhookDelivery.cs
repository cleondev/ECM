namespace ECM.Webhook.Domain;

public sealed class WebhookDelivery
{
    public Guid Id { get; set; }

    public string RequestId { get; set; } = default!;
    public string EndpointKey { get; set; } = default!;

    public string PayloadJson { get; set; } = default!;
    public string? CorrelationId { get; set; }

    public int AttemptCount { get; set; }

    // "Pending", "Succeeded", "Failed"
    public string Status { get; set; } = "Pending";

    public DateTimeOffset? LastAttemptAt { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public void RecordAttempt(DateTimeOffset timestamp)
    {
        AttemptCount++;
        LastAttemptAt = timestamp;
    }

    public void MarkSucceeded(DateTimeOffset timestamp)
    {
        Status = "Succeeded";
        LastAttemptAt = timestamp;
        LastError = null;
    }

    public void MarkFailed(DateTimeOffset timestamp, string? error = null)
    {
        Status = "Failed";
        LastAttemptAt = timestamp;
        LastError = error;
    }
}
