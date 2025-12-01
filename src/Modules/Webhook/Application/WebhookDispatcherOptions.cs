using System.Collections.Generic;

namespace ECM.Webhook.Application;

public sealed class WebhookDispatcherOptions
{
    public const string SectionName = "WebhookDispatcher";

    public int MaxRetryAttempts { get; set; } = 3;

    public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromSeconds(2);

    public IDictionary<string, string> Endpoints { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
