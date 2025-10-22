using AppGateway.Infrastructure.Ecm;
using Xunit;

namespace AppGateway.Infrastructure.Tests;

public static class ScopeUtilitiesTests
{
    [Fact]
    public static void ParseScopes_ReturnsEmptyArray_WhenScopeIsNull()
    {
        var result = ScopeUtilities.ParseScopes(null);

        Assert.Empty(result);
    }

    [Fact]
    public static void ParseScopes_EliminatesDuplicatesAndWhitespace()
    {
        const string scope = " api://istsvn.onmicrosoft.com/ecm-host/.default  api://istsvn.onmicrosoft.com/ecm-host/.default \nuser.read;offline_access,profile";

        var result = ScopeUtilities.ParseScopes(scope);

        Assert.Contains("api://istsvn.onmicrosoft.com/ecm-host", result);
        Assert.Contains("user.read", result);
        Assert.Contains("offline_access", result);
        Assert.Contains("profile", result);
        Assert.Equal(4, result.Length);
    }
}
