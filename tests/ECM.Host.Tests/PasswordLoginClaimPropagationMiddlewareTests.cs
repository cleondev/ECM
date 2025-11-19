using System.Security.Claims;
using ECM.Host.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Extensions.Http;
using Xunit;

namespace ECM.Host.Tests;

public class PasswordLoginClaimPropagationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AddsClaims_WhenOnlyForwardedHeadersAreAvailable()
    {
        var context = new DefaultHttpContext();
        var userId = Guid.NewGuid();

        context.Request.Headers[PasswordLoginForwardingHeaders.UserId] = userId.ToString();
        context.Request.Headers[PasswordLoginForwardingHeaders.Email] = "user@example.com";
        context.Request.Headers[PasswordLoginForwardingHeaders.DisplayName] = "Password Login";
        context.Request.Headers[PasswordLoginForwardingHeaders.PrimaryGroupId] = Guid.NewGuid().ToString();

        var middleware = new PasswordLoginClaimPropagationMiddleware(_ => Task.CompletedTask, NullLogger<PasswordLoginClaimPropagationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var principal = context.User;
        Assert.NotNull(principal);
        Assert.Equal(userId.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("user@example.com", principal.FindFirstValue(ClaimTypes.Email));
        Assert.Equal("Password Login", principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal("user@example.com", principal.FindFirstValue(ClaimTypes.Upn));
        Assert.Equal("user@example.com", principal.FindFirstValue("preferred_username"));
    }

    [Fact]
    public async Task InvokeAsync_LeavesExistingIdentityIntact_WhenNoForwardedHeaders()
    {
        var identity = new ClaimsIdentity("TestAuth");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        var middleware = new PasswordLoginClaimPropagationMiddleware(_ => Task.CompletedTask, NullLogger<PasswordLoginClaimPropagationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Same(identity, context.User.Identity);
    }
}
