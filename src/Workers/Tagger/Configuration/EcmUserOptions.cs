namespace Tagger.Configuration;

public sealed class EcmUserOptions
{
    public const string SectionName = "EcmUser";

    /// <summary>
    /// User identity (email or cloud id) that the Tagger worker should use when calling the ECM SDK.
    /// </summary>
    public string UserKey { get; init; } = "system@local";
}
