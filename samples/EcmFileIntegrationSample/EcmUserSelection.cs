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
        destination.AccessToken = source.AccessToken;
        destination.DocType = source.DocType;
        destination.Status = source.Status;
        destination.Sensitivity = source.Sensitivity;
        destination.OwnerId = source.OwnerId;
        destination.CreatedBy = source.CreatedBy;
        destination.DocumentTypeId = source.DocumentTypeId;
        destination.Title = source.Title;

        destination.OnBehalf.Enabled = source.OnBehalf.Enabled;
        destination.OnBehalf.ApiKey = source.OnBehalf.ApiKey;
        destination.OnBehalf.UserEmail = source.OnBehalf.UserEmail;
        destination.OnBehalf.UserId = source.OnBehalf.UserId;

        destination.OnBehalf.Sso.Enabled = source.OnBehalf.Sso.Enabled;
        destination.OnBehalf.Sso.Authority = source.OnBehalf.Sso.Authority;
        destination.OnBehalf.Sso.ClientId = source.OnBehalf.Sso.ClientId;
        destination.OnBehalf.Sso.ClientSecret = source.OnBehalf.Sso.ClientSecret;
        destination.OnBehalf.Sso.UserAccessToken = source.OnBehalf.Sso.UserAccessToken;
        destination.OnBehalf.Sso.Scopes = source.OnBehalf.Sso.Scopes?.ToArray() ?? Array.Empty<string>();
    }
}
