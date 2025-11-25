using ECM.Abstractions;
using ECM.IAM.Api.Auth;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using ServiceDefaults.Authentication;

namespace ECM.Host.Auth;

internal static class AuthenticationConfigurationExtensions
{
    public static IServiceCollection AddHostAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ApiKeyOptions>(configuration.GetSection(ApiKeyOptions.SectionName));

        var authenticationBuilder = services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = AuthenticationSchemeNames.BearerOrApiKey;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddPolicyScheme(
                AuthenticationSchemeNames.BearerOrApiKey,
                AuthenticationSchemeNames.BearerOrApiKey,
                options =>
                {
                    options.ForwardDefaultSelector = context =>
                        context.Request.Headers.ContainsKey(ApiKeyAuthenticationHandler.HeaderName)
                            ? ApiKeyAuthenticationHandler.AuthenticationScheme
                            : JwtBearerDefaults.AuthenticationScheme;
                })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationHandler.AuthenticationScheme,
                _ => { });

        authenticationBuilder.AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));

        var azureAdSection = configuration.GetSection("AzureAd");
        var azureInstance = azureAdSection["Instance"];
        var azureTenantId = azureAdSection["TenantId"];

        services.PostConfigure<MicrosoftIdentityOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options => options.Authority = AuthorityUtilities.EnsureV2Authority(
                options.Authority,
                options.TenantId,
                options.Instance));

        services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Authority = AuthorityUtilities.EnsureV2Authority(options.Authority, azureTenantId, azureInstance);
            options.Events ??= new JwtBearerEvents();
            var previousHandler = options.Events.OnTokenValidated;

            options.Events.OnTokenValidated = async context =>
            {
                if (previousHandler is not null)
                {
                    await previousHandler(context);
                }

                var provisioningService = context.HttpContext.RequestServices
                    .GetRequiredService<IUserProvisioningService>();

                await provisioningService.EnsureUserExistsAsync(
                    context.Principal,
                    context.HttpContext.RequestAborted);
            };
        });

        return services;
    }
}
