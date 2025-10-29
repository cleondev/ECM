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

        builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = AuthorityUtilities.EnsureV2Authority(options.Authority);
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true;
            options.ResponseMode = OpenIdConnectResponseMode.FormPost;
            options.Events ??= new OpenIdConnectEvents();

            foreach (var scope in ecmScopes)
            {
                if (!options.Scope.Contains(scope, StringComparer.Ordinal))
                {
                    options.Scope.Add(scope);
                }
            }

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

        return builder;
    }

    private static void ConfigureAuthentication(WebApplicationBuilder builder)
    {
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

        builder.Services.Configure<MicrosoftIdentityOptions>(
            OpenIdConnectDefaults.AuthenticationScheme,
            options => options.Authority = AuthorityUtilities.EnsureV2Authority(options.Authority));

        builder.Services.Configure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options => options.Authority = AuthorityUtilities.EnsureV2Authority(options.Authority));

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
