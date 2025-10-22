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
        const string scope = " api://istsvn.onmicrosoft.com/ecm-host/Access.All  api://istsvn.onmicrosoft.com/ecm-host/Access.All \nuser.read;offline_access,profile";

        var result = ScopeUtilities.ParseScopes(scope);

        Assert.Contains("api://istsvn.onmicrosoft.com/ecm-host/Access.All", result);
        Assert.Contains("user.read", result);
        Assert.Contains("offline_access", result);
        Assert.Contains("profile", result);
        Assert.Equal(4, result.Length);
    }

    [Theory]
    [InlineData("api://tenant.onmicrosoft.com/ecm-host/.default", "api://tenant.onmicrosoft.com/ecm-host/.default")]
    [InlineData("api://tenant.onmicrosoft.com/ecm-host/Access.All", "api://tenant.onmicrosoft.com/ecm-host/.default")]
    [InlineData("api://4b59d1fe-e515-4edf-905d-b09b04528fc3/Access.All", "api://4b59d1fe-e515-4edf-905d-b09b04528fc3/.default")]
    public static void TryGetAppScope_NormalizesScope(string input, string expected)
    {
        var result = ScopeUtilities.TryGetAppScope(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public static void TryGetAppScope_ReturnsNull_WhenScopeDoesNotRepresentResource()
    {
        var result = ScopeUtilities.TryGetAppScope("User.Read");

        Assert.Null(result);
    }
}
