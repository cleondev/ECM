using System;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Contracts.IAM.Users;
using AppGateway.Infrastructure.Auth;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AppGateway.Api.Auth;

public static class CurrentUserProfileResolver
{
    private const string ProfileResolutionItemKey = "__AppGateway:CurrentUserProfileResolution";

    public static async Task<CurrentUserProfileResolution> ResolveAsync(
        HttpContext httpContext,
        IEcmApiClient client,
        ILogger logger,
        CancellationToken cancellationToken,
        bool fetchFromApiWhenMissing = true)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);

        if (httpContext.Items.TryGetValue(ProfileResolutionItemKey, out var existing)
            && existing is CurrentUserProfileResolution cached)
        {
            if (fetchFromApiWhenMissing && cached.Status == CurrentUserProfileResolutionStatus.MissingProfile)
            {
                httpContext.Items.Remove(ProfileResolutionItemKey);
            }
            else
            {
                return cached;
            }
        }

        var principal = httpContext.User;
        if (PasswordLoginClaims.IsPasswordLoginPrincipal(principal))
        {
            var cachedProfile = PasswordLoginClaims.GetProfileFromPrincipal(principal, out var invalidProfileClaim);
            if (cachedProfile is not null)
            {
                return Store(httpContext, CurrentUserProfileResolution.FromCachedProfile(cachedProfile));
            }

            if (invalidProfileClaim)
            {
                logger.LogWarning(
                    "Password login principal had an invalid stored profile while resolving {Path}. Signing out.",
                    httpContext.Request.Path);
                return Store(httpContext, CurrentUserProfileResolution.InvalidPasswordLoginProfile);
            }
        }

        if (!fetchFromApiWhenMissing)
        {
            return Store(httpContext, CurrentUserProfileResolution.MissingProfile);
        }

        var profile = await client.GetCurrentUserProfileAsync(cancellationToken);
        return Store(httpContext, profile is null
            ? CurrentUserProfileResolution.NotFound
            : CurrentUserProfileResolution.FromRemoteProfile(profile));
    }

    private static CurrentUserProfileResolution Store(
        HttpContext httpContext,
        CurrentUserProfileResolution resolution)
    {
        httpContext.Items[ProfileResolutionItemKey] = resolution;
        return resolution;
    }
}

public sealed record CurrentUserProfileResolution(
    UserSummaryDto? Profile,
    CurrentUserProfileResolutionStatus Status)
{
    public bool RequiresSignOut => Status == CurrentUserProfileResolutionStatus.InvalidPasswordLoginProfile;

    public bool HasProfile => Profile is not null;

    public static CurrentUserProfileResolution MissingProfile { get; } =
        new(null, CurrentUserProfileResolutionStatus.MissingProfile);

    public static CurrentUserProfileResolution InvalidPasswordLoginProfile { get; } =
        new(null, CurrentUserProfileResolutionStatus.InvalidPasswordLoginProfile);

    public static CurrentUserProfileResolution NotFound { get; } =
        new(null, CurrentUserProfileResolutionStatus.NotFound);

    public static CurrentUserProfileResolution FromCachedProfile(UserSummaryDto profile)
        => new(profile, CurrentUserProfileResolutionStatus.ResolvedFromCache);

    public static CurrentUserProfileResolution FromRemoteProfile(UserSummaryDto profile)
        => new(profile, CurrentUserProfileResolutionStatus.ResolvedFromApi);
}

public enum CurrentUserProfileResolutionStatus
{
    MissingProfile,
    ResolvedFromCache,
    ResolvedFromApi,
    NotFound,
    InvalidPasswordLoginProfile
}
