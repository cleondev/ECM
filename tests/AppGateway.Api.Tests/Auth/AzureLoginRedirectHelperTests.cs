using AppGateway.Api.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AppGateway.Api.Tests.Auth;

public static class AzureLoginRedirectHelperTests
{
    [Fact]
    public static void ResolveRedirectPath_PreservesQueryAndFragment_ForCandidate()
    {
        var context = new DefaultHttpContext();

        var result = AzureLoginRedirectHelper.ResolveRedirectPath(
            context,
            "/app/library/?view=grid#section",
            "/app");

        result.Should().Be("/app/library?view=grid#section");
    }

    [Fact]
    public static void ResolveRedirectPath_PreservesSuffix_WhenPathBaseIsPresent()
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                PathBase = new PathString("/ecm")
            }
        };

        var result = AzureLoginRedirectHelper.ResolveRedirectPath(
            context,
            "/app/files/?view=list#details",
            "/app");

        result.Should().Be("/ecm/app/files?view=list#details");
    }

    [Fact]
    public static void ResolveRedirectPath_UsesDefaultWithSuffix_WhenCandidateInvalid()
    {
        var context = new DefaultHttpContext();

        var result = AzureLoginRedirectHelper.ResolveRedirectPath(
            context,
            "  ",
            "/app/settings/?tab=notifications#alerts");

        result.Should().Be("/app/settings?tab=notifications#alerts");
    }
}
