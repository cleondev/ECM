namespace AppGateway.Api.Auth;

public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKeys";

    public IReadOnlyList<ApiKeyEntry> Keys { get; init; } = [];

    public IReadOnlyCollection<string> AllowedKeys
    {
        get
        {
            var keys = Keys
                .Select(entry => entry.Key)
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key!.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            return keys;
        }
    }
}

public sealed record ApiKeyEntry
{
    public string? Name { get; init; }

    public string? Key { get; init; }
}
