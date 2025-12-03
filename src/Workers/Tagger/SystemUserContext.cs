using Ecm.Sdk.Authentication;

namespace Tagger;

internal sealed class SystemUserContext : IEcmUserContext
{
    public string GetUserKey() => "system@local";
}
