namespace ECM.Webhook.Domain;

public sealed class WebhookDelivery
{
    public Guid Id { get; set; }

    public string RequestId { get; set; } = default!;
    public string EndpointKey { get; set; } = default!;

    public int AttemptCount { get; set; }

    // "Pending", "Succeeded", "Failed"
    public string Status { get; set; } = "Pending";

    public DateTimeOffset LastAttemptAt { get; set; }

    public void RecordAttempt(DateTimeOffset timestamp)
    {
        AttemptCount++;
        LastAttemptAt = timestamp;
    }

    public void MarkSucceeded(DateTimeOffset timestamp)
    {
        Status = "Succeeded";
        LastAttemptAt = timestamp;
    }

    public void MarkFailed(DateTimeOffset timestamp)
    {
        Status = "Failed";
        LastAttemptAt = timestamp;
    }
}
