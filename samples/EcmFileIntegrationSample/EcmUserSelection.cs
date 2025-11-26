using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using Ecm.Sdk;

namespace samples.EcmFileIntegrationSample;

public sealed class EcmUserSelection
{
    private const string CookieName = "ecm-user";

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
        var key = _httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
        var user = _store.GetUserOrDefault(key);

        if (string.IsNullOrWhiteSpace(key) && _httpContextAccessor.HttpContext is { } context)
        {
            WriteSelectionCookie(context, user.Key);
        }

        return user;
    }

    public EcmIntegrationOptions GetCurrentOptions()
    {
        var user = GetCurrentUser();
        return _store.BuildOptions(user);
    }

    public void SelectUser(string? key)
    {
        if (_httpContextAccessor.HttpContext is not { } context)
        {
            return;
        }

        var selected = _store.GetUserOrDefault(key);
        WriteSelectionCookie(context, selected.Key);
    }

    private static void WriteSelectionCookie(HttpContext context, string key)
    {
        context.Response.Cookies.Append(
            CookieName,
            key,
            new CookieOptions
            {
                IsEssential = true,
                HttpOnly = false,
                Expires = DateTimeOffset.UtcNow.AddDays(30),
            });
    }
}

public sealed class EcmUserOptionsConfigurator : IConfigureOptions<EcmIntegrationOptions>
{
    private readonly EcmUserSelection _selection;

    public EcmUserOptionsConfigurator(EcmUserSelection selection)
    {
        _selection = selection;
    }

    public void Configure(EcmIntegrationOptions options)
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
