using System;
using System.Linq;
using AppGateway.Api.Auth;
using AppGateway.Infrastructure;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ServiceDefaults.Authentication;

namespace AppGateway.Api.Configuration;

public static class GatewayServiceConfiguration
{
    public static WebApplicationBuilder ConfigureGatewayServices(this WebApplicationBuilder builder)
    {
        ConfigureAuthentication(builder);

        builder.Services.AddProblemDetails();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddGatewayInfrastructure(builder.Configuration);
        builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection(ApiKeyOptions.SectionName));
        builder.Services.AddScoped<IUserProvisioningService, AzureAdUserProvisioningService>();

        var uploadLimitOptions = builder.Configuration
            .GetSection(UploadLimitOptions.SectionName)
            .Get<UploadLimitOptions>() ?? new UploadLimitOptions();

        uploadLimitOptions.EnsureValid();

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = uploadLimitOptions.MaxRequestBodySize;
        });

        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = uploadLimitOptions.MultipartBodyLengthLimit;
            options.ValueLengthLimit = int.MaxValue;
            options.MemoryBufferThreshold = int.MaxValue;
        });

        var ecmScopes = ScopeUtilities.ParseScopes(
            builder.Configuration.GetValue<string>("Services:EcmScope"));

        var azureAdSection = builder.Configuration.GetSection("AzureAd");
        var azureInstance = azureAdSection["Instance"];
        var azureTenantId = azureAdSection["TenantId"];

        builder.Services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = AuthorityUtilities.EnsureV2Authority(options.Authority, azureTenantId, azureInstance);
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true;
            options.ResponseMode = OpenIdConnectResponseMode.FormPost;
            options.Events ??= new OpenIdConnectEvents();

            if (!options.Scope.Contains("email", StringComparer.OrdinalIgnoreCase))
            {
                options.Scope.Add("email");
            }

            foreach (var scope in ecmScopes)
            {
                if (!options.Scope.Contains(scope, StringComparer.Ordinal))
                {
                    options.Scope.Add(scope);
                }
            }

            var previousTokenValidatedHandler = options.Events.OnTokenValidated;

            options.Events.OnTokenValidated = async context =>
            {
                if (previousTokenValidatedHandler is not null)
                {
                    await previousTokenValidatedHandler(context);
                }

                var provisioningService = context.HttpContext.RequestServices
                    .GetRequiredService<IUserProvisioningService>();

                await provisioningService.EnsureUserExistsAsync(
                    context.Principal,
                    context.HttpContext.RequestAborted);
            };

            var previousRedirectHandler = options.Events.OnRedirectToIdentityProvider;

            options.Events.OnRedirectToIdentityProvider = async context =>
            {
                if (previousRedirectHandler is not null)
                {
                    await previousRedirectHandler(context);
                    if (context.Handled)
                    {
                        return;
                    }
                }

                var loginMode = AzureLoginRedirectHelper.ResolveLoginMode(context.Properties);

                if (loginMode == AzureLoginRedirectHelper.AzureLoginMode.Silent)
                {
                    context.ProtocolMessage.Prompt = OpenIdConnectPrompt.None;
                }
            };

            var previousRemoteFailureHandler = options.Events.OnRemoteFailure;

            options.Events.OnRemoteFailure = async context =>
            {
                if (AzureLoginRedirectHelper.ResolveLoginMode(context.Properties)
                    == AzureLoginRedirectHelper.AzureLoginMode.Silent)
                {
                    context.HandleResponse();

                    var redirectUri = context.Properties?.RedirectUri;

                    if (!string.IsNullOrWhiteSpace(redirectUri))
                    {
                        context.Response.Redirect(redirectUri);
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status204NoContent;
                    }

                    return;
                }

                if (previousRemoteFailureHandler is not null)
                {
                    await previousRemoteFailureHandler(context);
                }
            };
        });

        return builder;
    }

    private static void ConfigureAuthentication(WebApplicationBuilder builder)
    {
        var azureAdSection = builder.Configuration.GetSection("AzureAd");
        var azureInstance = azureAdSection["Instance"];
        var azureTenantId = azureAdSection["TenantId"];

        var authenticationBuilder = builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        });

        authenticationBuilder.AddMicrosoftIdentityWebApp(
                builder.Configuration.GetSection("AzureAd"),
                cookieScheme: CookieAuthenticationDefaults.AuthenticationScheme,
                openIdConnectScheme: OpenIdConnectDefaults.AuthenticationScheme)
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();

        authenticationBuilder.AddMicrosoftIdentityWebApi(
            builder.Configuration.GetSection("AzureAd"),
            jwtBearerScheme: JwtBearerDefaults.AuthenticationScheme);

        builder.Services.PostConfigure<MicrosoftIdentityOptions>(
            OpenIdConnectDefaults.AuthenticationScheme,
            options => options.Authority = AuthorityUtilities.EnsureV2Authority(
                options.Authority,
                options.TenantId,
                options.Instance));

        builder.Services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options =>
            {
                options.Authority = AuthorityUtilities.EnsureV2Authority(
                    options.Authority,
                    azureTenantId,
                    azureInstance);
            });

        authenticationBuilder.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationHandler.AuthenticationScheme,
            _ => { });

        builder.Services.Configure<CookieAuthenticationOptions>(
            CookieAuthenticationDefaults.AuthenticationScheme,
            options =>
            {
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
            });

        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(
                    JwtBearerDefaults.AuthenticationScheme,
                    ApiKeyAuthenticationHandler.AuthenticationScheme,
                    CookieAuthenticationDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build());
    }
}
