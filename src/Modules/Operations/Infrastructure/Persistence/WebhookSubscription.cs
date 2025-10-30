using System;
using System.Collections.Generic;
using System.Linq;

namespace ECM.Operations.Infrastructure.Persistence;

public sealed class WebhookSubscription
{
    private WebhookSubscription()
    {
        EventTypes = [];
        Url = string.Empty;
        Secret = string.Empty;
        Description = string.Empty;
    }

    public WebhookSubscription(string name, IEnumerable<string> eventTypes, string url, string secret, string description, bool isActive, DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        Name = name;
        EventTypes = [.. eventTypes.Distinct()];
        Url = url;
        Secret = secret;
        Description = description;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string[] EventTypes { get; private set; }

    public string Url { get; private set; }

    public string Secret { get; private set; }

    public string Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? DeactivatedAtUtc { get; private set; }

    public void Deactivate(DateTimeOffset occurredAtUtc)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        DeactivatedAtUtc = occurredAtUtc;
    }
}
