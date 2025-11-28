using Ecm.Sdk.Authentication;

using EcmFileIntegrationSample;

namespace EcmFileIntegrationSample;
public sealed class EnvEcmUserContext(EcmUserSelection userSelection) : IEcmUserContext
{
    private readonly EcmUserSelection _userSelection = userSelection;

    public string GetUserKey()
    {
        return _userSelection.GetCurrentUser().Email ??  "sonlh@ists.com.vn";
    }
}