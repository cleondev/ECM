using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ECM.Host.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace ECM.Host.Tests;

public class ApiKeyAuthenticationHandlerTests
{
    [Fact]
    public async Task AuthenticateAsync_Succeeds_WhenValidApiKeyProvided()
    {
        var services = BuildServices(options =>
        {
            options.Keys = [new ApiKeyEntry { Name = "Gateway", Key = "secret" }];
        });

        var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Headers[ApiKeyAuthenticationHandler.HeaderName] = "secret";

        var result = await context.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(ApiKeyAuthenticationHandler.AuthenticationScheme, result.Ticket?.AuthenticationScheme);
        Assert.Equal("Gateway", result.Principal?.Identity?.Name);
    }

    [Fact]
    public async Task AuthenticateAsync_ForwardsToJwtBearer_WhenNoApiKeyIsPresent()
    {
        var services = BuildServices(options =>
        {
            options.Keys = [new ApiKeyEntry { Name = "Gateway", Key = "secret" }];
        });

        var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };

        var result = await context.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, result.Ticket?.AuthenticationScheme);
        Assert.Equal("jwt-user", result.Principal?.Identity?.Name);
    }

    private static IServiceCollection BuildServices(Action<ApiKeyOptions> configureApiKeys)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure(configureApiKeys);

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = AuthenticationSchemeNames.BearerOrApiKey;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddPolicyScheme(AuthenticationSchemeNames.BearerOrApiKey, AuthenticationSchemeNames.BearerOrApiKey, options =>
        {
            options.ForwardDefaultSelector = context =>
                context.Request.Headers.ContainsKey(ApiKeyAuthenticationHandler.HeaderName)
                    ? ApiKeyAuthenticationHandler.AuthenticationScheme
                    : JwtBearerDefaults.AuthenticationScheme;
        })
        .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationHandler.AuthenticationScheme,
            _ => { })
        .AddScheme<AuthenticationSchemeOptions, FakeJwtAuthenticationHandler>(
            JwtBearerDefaults.AuthenticationScheme,
            _ => { });

        return services;
    }

    private sealed class FakeJwtAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public FakeJwtAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(JwtBearerDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, "jwt-user"));

            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, JwtBearerDefaults.AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
