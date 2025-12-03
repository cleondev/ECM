using ECM.Document.Api.Shares;

using Microsoft.AspNetCore.Http;

using Xunit;

namespace File.Tests.Api.Shares;

public class ShareEndpointsTests
{
    [Fact]
    public void ResolveRequestBaseUrl_WithoutForwardedHeaders_UsesRequestHost()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost", 8080);

        var result = ShareEndpoints.ResolveRequestBaseUrl(context.Request);

        Assert.Equal("https://localhost:8080", result);
    }

    [Fact]
    public void ResolveRequestBaseUrl_WithForwardedHeaders_UsesForwardedValues()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("internal", 8080);
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-Host"] = "files.example.com";
        context.Request.Headers["X-Forwarded-Port"] = "5090";

        var result = ShareEndpoints.ResolveRequestBaseUrl(context.Request);

        Assert.Equal("https://files.example.com:5090", result);
    }

    [Fact]
    public void ResolveRequestBaseUrl_WithForwardedPrefix_AppendsPrefix()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-Host"] = "files.example.com";
        context.Request.Headers["X-Forwarded-Prefix"] = "/gateway";

        var result = ShareEndpoints.ResolveRequestBaseUrl(context.Request);

        Assert.Equal("https://files.example.com/gateway", result);
    }

    [Fact]
    public void ResolveRequestBaseUrl_WithMultipleForwardedValues_UsesFirstEntry()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("internal", 8080);
        context.Request.Headers["X-Forwarded-Proto"] = "https, http";
        context.Request.Headers["X-Forwarded-Host"] = "files.example.com, internal";
        context.Request.Headers["X-Forwarded-Port"] = "5090, 80";

        var result = ShareEndpoints.ResolveRequestBaseUrl(context.Request);

        Assert.Equal("https://files.example.com:5090", result);
    }

    [Fact]
    public void ResolveRequestBaseUrl_DefaultPort_OmitsPort()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("internal", 8080);
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-Host"] = "files.example.com";
        context.Request.Headers["X-Forwarded-Port"] = "443";

        var result = ShareEndpoints.ResolveRequestBaseUrl(context.Request);

        Assert.Equal("https://files.example.com", result);
    }
}
