using System.Security.Claims;

using Ecm.Sdk.Configuration;

using Microsoft.Extensions.Options;

namespace EcmFileIntegrationSample;

public sealed class EcmUserSelection(IHttpContextAccessor httpContextAccessor, EcmUserStore store)
{
    private const string ContextItemKey = "ecm-user-selected";

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly EcmUserStore _store = store;

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

        context.User = BuildPrincipal(selected);

        return selected;
    }

    private EcmUserConfiguration ResolveUser(string? email)
    {
        return _store.GetUserByEmailOrDefault(email);
    }

    private static ClaimsPrincipal BuildPrincipal(EcmUserConfiguration selected)
    {
        var claims = new List<Claim>();

        if (!string.IsNullOrWhiteSpace(selected.Email))
        {
            claims.Add(new Claim("email", selected.Email));
            claims.Add(new Claim(ClaimTypes.Email, selected.Email));
            claims.Add(new Claim(ClaimTypes.Name, selected.Email));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "ecm-sample");
        return new ClaimsPrincipal(identity);
    }
}

public sealed class EcmUserOptionsConfigurator(EcmUserSelection selection) : IPostConfigureOptions<EcmIntegrationOptions>
{
    private readonly EcmUserSelection _selection = selection;

    public void PostConfigure(string? name, EcmIntegrationOptions options)
    {
        var current = _selection.GetCurrentOptions();
        Copy(current, options);
    }

    public static void Copy(EcmIntegrationOptions source, EcmIntegrationOptions destination)
    {
        destination.BaseUrl = source.BaseUrl;

        destination.ApiKey.Enabled = source.ApiKey.Enabled;
        destination.ApiKey.ApiKey = source.ApiKey.ApiKey;

        destination.Sso.Enabled = source.Sso.Enabled;
        destination.Sso.Authority = source.Sso.Authority;
        destination.Sso.ClientId = source.Sso.ClientId;
        destination.Sso.ClientSecret = source.Sso.ClientSecret;
        destination.Sso.Scopes = source.Sso.Scopes?.ToArray() ?? [];
    }
}
