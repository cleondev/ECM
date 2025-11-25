using System;
using System.Collections.Generic;

namespace ECM.Host.Auth;

internal sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKeys";

    public IReadOnlyList<ApiKeyEntry> Keys { get; set; } = Array.Empty<ApiKeyEntry>();
}

internal sealed record ApiKeyEntry
{
    public string? Name { get; init; }

    public string? Key { get; init; }
}
