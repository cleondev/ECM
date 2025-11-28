using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ECM.Abstractions.Security;
using ECM.Document.Application.UserContext;
using Xunit;

namespace Document.Tests.Application.UserContext;

public class DocumentUserContextResolverTests
{
    [Fact]
    public void Resolve_WithExplicitIds_PrefersCommandValues()
    {
        var ownerId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var resolver = new DocumentUserContextResolver(new FakeCurrentUserProvider(Guid.NewGuid(), "Principal User"));

        var context = resolver.Resolve(new DocumentCommandContext(ownerId, createdBy));

        Assert.Equal(ownerId, context.OwnerId);
        Assert.Equal(createdBy, context.CreatedBy);
        Assert.Equal("Principal User", context.DisplayName);
    }

    [Fact]
    public void Resolve_WhenCommandMissingIds_UsesCurrentUser()
    {
        var userId = Guid.NewGuid();
        var resolver = new DocumentUserContextResolver(new FakeCurrentUserProvider(userId, "Current User"));

        var context = resolver.Resolve(new DocumentCommandContext(null, null));

        Assert.Equal(userId, context.OwnerId);
        Assert.Equal(userId, context.CreatedBy);
        Assert.Equal("Current User", context.DisplayName);
    }

    [Fact]
    public void Resolve_WhenNoIdsAvailable_ThrowsValidationException()
    {
        var resolver = new DocumentUserContextResolver(new FakeCurrentUserProvider(null, null));

        Assert.Throws<ValidationException>(() => resolver.Resolve(new DocumentCommandContext(null, null)));
    }

    private sealed class FakeCurrentUserProvider(Guid? userId, string? displayName) : ICurrentUserProvider
    {
        public ClaimsPrincipal? Current => null;

        public Guid? TryGetUserId() => userId;

        public string? TryGetDisplayName() => displayName;
    }
}
