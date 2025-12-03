namespace Ecm.Sdk.Authentication;

using System.Threading;

/// <summary>
/// Manual user context for non-HTTP scenarios (e.g., console or worker apps).
/// </summary>
/// <remarks>
/// Consumers can call <see cref="SetUserKey"/> to establish the identity used by the SDK
/// for subsequent requests executed on the current async flow.
/// </remarks>
public sealed class ManualEcmUserContext : IEcmUserContext
{
    private static readonly AsyncLocal<string?> CurrentUserKey = new();

    /// <summary>
    /// Sets the user key for the current async context.
    /// </summary>
    /// <param name="userKey">Identity value (email/cloud id) used when acquiring tokens.</param>
    public static void SetUserKey(string userKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userKey);
        CurrentUserKey.Value = userKey;
    }

    /// <summary>
    /// Clears any user key previously set on the current async context.
    /// </summary>
    public static void Clear() => CurrentUserKey.Value = null;

    /// <inheritdoc />
    public string GetUserKey() => CurrentUserKey.Value
        ?? throw new InvalidOperationException(
            "ManualEcmUserContext requires a user key to be set via SetUserKey before use.");
}
