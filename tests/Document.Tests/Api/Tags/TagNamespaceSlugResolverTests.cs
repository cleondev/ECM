using System.Security.Claims;
using ECM.Document.Api.Tags;
using Xunit;

namespace Document.Tests.Api.Tags;

public class TagNamespaceSlugResolverTests
{
    [Theory]
    [InlineData("system", "system")]
    [InlineData("SYSTEM", "system")]
    [InlineData("user/jane@example.com", "user/jane@example.com")]
    public void Resolve_WithNonUserNamespace_ReturnsNormalizedSlug(string input, string expected)
    {
        var principal = new ClaimsPrincipal();

        var result = TagNamespaceSlugResolver.Resolve(input, principal);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_WithUserNamespaceAndEmailClaim_AppendsEmail()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, "Jane.Doe@Example.com") });
        var principal = new ClaimsPrincipal(identity);

        var result = TagNamespaceSlugResolver.Resolve("user", principal);

        Assert.Equal("user/jane.doe@example.com", result);
    }

    [Fact]
    public void Resolve_WithUserNamespaceAndPreferredUsername_AppendsUsername()
    {
        var identity = new ClaimsIdentity(new[] { new Claim("preferred_username", "User.One@Example.com") });
        var principal = new ClaimsPrincipal(identity);

        var result = TagNamespaceSlugResolver.Resolve("user", principal);

        Assert.Equal("user/user.one@example.com", result);
    }

    [Fact]
    public void Resolve_WithUserNamespaceWithoutEmail_ReturnsUserSlug()
    {
        var principal = new ClaimsPrincipal();

        var result = TagNamespaceSlugResolver.Resolve("user", principal);

        Assert.Equal("user", result);
    }

    [Fact]
    public void Resolve_WithNullNamespace_ReturnsEmptyString()
    {
        var principal = new ClaimsPrincipal();

        var result = TagNamespaceSlugResolver.Resolve(null, principal);

        Assert.Equal(string.Empty, result);
    }
}
