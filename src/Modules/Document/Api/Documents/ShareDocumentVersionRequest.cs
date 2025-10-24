using System.ComponentModel.DataAnnotations;

namespace ECM.Document.Api.Documents;

public sealed class ShareDocumentVersionRequest
{
    private const int DefaultLifetimeMinutes = 1440; // 24 hours
    private const int MinLifetimeMinutes = 1;
    private const int MaxLifetimeMinutes = 10080; // 7 days

    [Range(MinLifetimeMinutes, MaxLifetimeMinutes, ErrorMessage = "Share duration must be between {1} and {2} minutes.")]
    public int? ExpiresInMinutes { get; init; }

    public bool IsPublic { get; init; }

    public int GetEffectiveMinutes()
    {
        if (ExpiresInMinutes is null)
        {
            return DefaultLifetimeMinutes;
        }

        if (ExpiresInMinutes.Value < MinLifetimeMinutes)
        {
            return MinLifetimeMinutes;
        }

        if (ExpiresInMinutes.Value > MaxLifetimeMinutes)
        {
            return MaxLifetimeMinutes;
        }

        return ExpiresInMinutes.Value;
    }

    public static int Minimum => MinLifetimeMinutes;

    public static int Maximum => MaxLifetimeMinutes;
}
