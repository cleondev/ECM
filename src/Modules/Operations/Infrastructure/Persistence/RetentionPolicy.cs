using System;

namespace ECM.Operations.Infrastructure.Persistence;

public sealed class RetentionPolicy
{
    private RetentionPolicy()
    {
        Name = string.Empty;
        Rule = string.Empty;
    }

    public RetentionPolicy(string name, string rule, bool isActive, DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        Name = name;
        Rule = rule;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Rule { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
