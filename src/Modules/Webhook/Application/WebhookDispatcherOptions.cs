namespace ECM.Webhook.Application;

public sealed class WebhookDispatcherOptions
{
    public const string SectionName = "Webhook";

    public int MaxRetryAttempts { get; set; } = 3;

    public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromSeconds(2);

    public IList<WebhookEndpointOptions> Endpoints { get; set; } = new List<WebhookEndpointOptions>();
}

public sealed class WebhookEndpointOptions
{
    public string Key { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string HttpMethod { get; set; } = System.Net.Http.HttpMethod.Post.Method;
}
