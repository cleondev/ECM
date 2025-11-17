using System;
using System.Threading.Tasks;
using AppGateway.Contracts.IAM.Users;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace AppGateway.Api.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireCurrentUserProfileAttribute(bool fetchFromApiWhenMissing = true)
    : TypeFilterAttribute(typeof(RequireCurrentUserProfileFilter))
{
    public RequireCurrentUserProfileAttribute()
        : this(true)
    {
    }

    public RequireCurrentUserProfileAttribute(bool fetchFromApiWhenMissing)
        : base(typeof(RequireCurrentUserProfileFilter))
    {
        Arguments = new object[] { fetchFromApiWhenMissing };
    }

    private sealed class RequireCurrentUserProfileFilter(
        IEcmApiClient client,
        ILogger<RequireCurrentUserProfileFilter> logger,
        bool fetchFromApiWhenMissing) : IAsyncActionFilter
    {
        private readonly IEcmApiClient _client = client;
        private readonly ILogger<RequireCurrentUserProfileFilter> _logger = logger;
        private readonly bool _fetchFromApiWhenMissing = fetchFromApiWhenMissing;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var resolution = await CurrentUserProfileResolver.ResolveAsync(
                httpContext,
                _client,
                _logger,
                httpContext.RequestAborted,
                _fetchFromApiWhenMissing);

            if (resolution.RequiresSignOut)
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                context.Result = new UnauthorizedResult();
                return;
            }

            var profile = resolution.Profile;
            if (profile is null)
            {
                if (resolution.Status == CurrentUserProfileResolutionStatus.NotFound)
                {
                    context.Result = new NotFoundResult();
                    return;
                }

                context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                return;
            }

            CurrentUserProfileStore.Set(httpContext, profile);

            await next();
        }
    }
}

public static class CurrentUserProfileStore
{
    private const string ProfileItemKey = "__AppGateway:CurrentUserProfile";

    public static bool TryGet(HttpContext context, out UserSummaryDto? profile)
    {
        if (context.Items.TryGetValue(ProfileItemKey, out var value) && value is UserSummaryDto dto)
        {
            profile = dto;
            return true;
        }

        profile = null;
        return false;
    }

    public static void Set(HttpContext context, UserSummaryDto profile)
    {
        context.Items[ProfileItemKey] = profile;
    }
}
