using ECM.IAM.Domain.Roles;
using ECM.IAM.Domain.Users;
using TestFixtures;
using Xunit;

namespace IAM.Test.Domain.Users;

public class UserTests
{
    private readonly DefaultGroupFixture _groups = new();

    [Fact]
    public void Create_WithValidValues_TrimsStringsAndSetsDefaults()
    {
        var createdAt = DateTimeOffset.UtcNow;

        var user = User.Create("  alice@example.com  ", "  Alice Smith  ", createdAt, isActive: false);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("alice@example.com", user.Email);
        Assert.Equal("Alice Smith", user.DisplayName);
        Assert.Null(user.Department);
        Assert.False(user.IsActive);
        Assert.Equal(createdAt, user.CreatedAtUtc);
        Assert.Empty(user.Roles);
    }

    [Theory]
    [InlineData(null, "Name")]
    [InlineData("", "Name")]
    [InlineData("   ", "Name")]
    [InlineData("user@example.com", null)]
    [InlineData("user@example.com", "")]
    [InlineData("user@example.com", "   ")]
    public void Create_WithMissingRequiredFields_ThrowsArgumentException(string? email, string? displayName)
    {
        var createdAt = DateTimeOffset.UtcNow;

        var exception = Assert.Throws<ArgumentException>(() => User.Create(email!, displayName!, createdAt));
        if (string.IsNullOrWhiteSpace(email))
        {
            Assert.StartsWith("Email is required.", exception.Message);
            Assert.Equal("email", exception.ParamName);
        }
        else
        {
            Assert.StartsWith("Display name is required.", exception.Message);
            Assert.Equal("displayName", exception.ParamName);
        }
    }

    [Fact]
    public void UpdateDisplayName_WithValidValue_TrimsAndUpdates()
    {
        var user = User.Create("user@example.com", "User", DateTimeOffset.UtcNow);

        user.UpdateDisplayName("  Updated Name  ");

        Assert.Equal("Updated Name", user.DisplayName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDisplayName_WithMissingValue_ThrowsArgumentException(string? name)
    {
        var user = User.Create("user@example.com", "User", DateTimeOffset.UtcNow);

        var exception = Assert.Throws<ArgumentException>(() => user.UpdateDisplayName(name!));
        Assert.StartsWith("Display name is required.", exception.Message);
        Assert.Equal("displayName", exception.ParamName);
    }

    [Fact]
    public void UpdateDepartment_TrimsValueOrClearsWhenWhitespace()
    {
    [Fact]
    public void ActivateAndDeactivate_ToggleState()
    {
        var user = User.Create("user@example.com", "User", DateTimeOffset.UtcNow, isActive: false);

        user.Activate();
        Assert.True(user.IsActive);

        user.Deactivate();
        Assert.False(user.IsActive);
    }

    [Fact]
    public void AssignRole_WithValidRole_AddsLink()
    {
        var user = User.Create("user@example.com", "User", DateTimeOffset.UtcNow);
        var role = Role.Create("Reviewer");

        user.AssignRole(role, DateTimeOffset.UtcNow);

        var link = Assert.Single(user.Roles);
        Assert.Equal(user.Id, link.UserId);
        Assert.Equal(role.Id, link.RoleId);
        Assert.Equal(role, link.Role);
    }

    [Fact]
    public void AssignRole_WithDuplicateRole_DoesNotAddSecondLink()
    {
        var user = User.Create("user@example.com", "User", DateTimeOffset.UtcNow);
        var role = Role.Create("Reviewer");

        user.AssignRole(role, DateTimeOffset.UtcNow);
        user.AssignRole(role, DateTimeOffset.UtcNow);

        Assert.Single(user.Roles);
    }

    [Fact]
    public void AssignRole_WithNullRole_ThrowsArgumentNullException()
    {
        var user = User.Create("user@example.com", "User", DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentNullException>(() => user.AssignRole(null!, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RemoveRole_WhenRoleExists_RemovesLink()
    {
        var user = User.Create("user@example.com", "User", DateTimeOffset.UtcNow);
        var role = Role.Create("Reviewer");
        user.AssignRole(role, DateTimeOffset.UtcNow);

        user.RemoveRole(role.Id, DateTimeOffset.UtcNow);

        Assert.Empty(user.Roles);
    }

    [Fact]
    public void RemoveRole_WhenRoleMissing_DoesNothing()
    {
        var user = User.Create("user@example.com", "User", DateTimeOffset.UtcNow);
        var role = Role.Create("Reviewer");
        user.AssignRole(role, DateTimeOffset.UtcNow);

        user.RemoveRole(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Single(user.Roles);
    }
}
