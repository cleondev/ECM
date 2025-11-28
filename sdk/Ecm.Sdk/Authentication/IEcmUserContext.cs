namespace Ecm.Sdk.Authentication;

/// <summary>
///  IEcmUserContext
/// </summary>
public interface IEcmUserContext
{
    /// <summary>
    /// Return the user key (CloudId/email/whatever) that
    /// ECM should use for authentication / session.
    /// </summary>
    string GetUserKey();
}
