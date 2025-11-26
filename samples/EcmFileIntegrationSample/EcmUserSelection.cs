using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using Ecm.Sdk;

namespace samples.EcmFileIntegrationSample;

public sealed class EcmUserSelection
{
    private const string ContextItemKey = "ecm-user-selected";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly EcmUserStore _store;

    public EcmUserSelection(IHttpContextAccessor httpContextAccessor, EcmUserStore store)
    {
        _httpContextAccessor = httpContextAccessor;
        _store = store;
    }

    public IReadOnlyCollection<EcmUserConfiguration> GetUsers() => _store.Users;

    public EcmUserConfiguration GetCurrentUser()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Items[ContextItemKey] is EcmUserConfiguration requestedUser)
        {
            return requestedUser;
        }

        return _store.DefaultUser;
    }

    public EcmIntegrationOptions GetCurrentOptions()
    {
        var user = GetCurrentUser();
        return _store.BuildOptions(user);
    }

    public EcmUserConfiguration ApplySelection(EcmIntegrationOptions options, string? userEmail)
    {
        var selected = SelectUser(userEmail);
        var configured = _store.BuildOptions(selected);
        EcmUserOptionsConfigurator.Copy(configured, options);
        return selected;
    }

    public EcmUserConfiguration SelectUser(string? email = null)
    {
        if (_httpContextAccessor.HttpContext is not { } context)
        {
            return ResolveUser(email);
        }

        var selected = ResolveUser(email);
        context.Items[ContextItemKey] = selected;
        return selected;
    }

    private EcmUserConfiguration ResolveUser(string? email)
    {
        return _store.GetUserByEmailOrDefault(email);
    }
}

public sealed class EcmUserOptionsConfigurator : IPostConfigureOptions<EcmIntegrationOptions>
{
    private readonly EcmUserSelection _selection;

    public EcmUserOptionsConfigurator(EcmUserSelection selection)
    {
        _selection = selection;
    }

    public void PostConfigure(string? name, EcmIntegrationOptions options)
    {
        var current = _selection.GetCurrentOptions();
        Copy(current, options);
    }

    public static void Copy(EcmIntegrationOptions source, EcmIntegrationOptions destination)
    {
        destination.BaseUrl = source.BaseUrl;
        destination.DocType = source.DocType;
        destination.Status = source.Status;
        destination.Sensitivity = source.Sensitivity;
        destination.OwnerId = source.OwnerId;
        destination.CreatedBy = source.CreatedBy;
        destination.DocumentTypeId = source.DocumentTypeId;
        destination.Title = source.Title;

        destination.ApiKey.Enabled = source.ApiKey.Enabled;
        destination.ApiKey.ApiKey = source.ApiKey.ApiKey;
        destination.OnBehalfUserEmail = source.OnBehalfUserEmail;
        destination.OnBehalfUserId = source.OnBehalfUserId;

        destination.Sso.Enabled = source.Sso.Enabled;
        destination.Sso.Authority = source.Sso.Authority;
        destination.Sso.ClientId = source.Sso.ClientId;
        destination.Sso.ClientSecret = source.Sso.ClientSecret;
        destination.Sso.UserAccessToken = source.Sso.UserAccessToken;
        destination.Sso.Scopes = source.Sso.Scopes?.ToArray() ?? Array.Empty<string>();
    }
}
