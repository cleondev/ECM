using System.Net;
using System.Security.Claims;
using System.Text.Json;

using AppGateway.Api.Controllers.IAM;
using AppGateway.Contracts.IAM.Users;
using AppGateway.Infrastructure.Auth;
using AppGateway.Infrastructure.Ecm;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Xunit;

namespace AppGateway.Api.Tests.Controllers;

public class IamUserProfileControllerTests
{
    [Fact]
    public async Task GetAsync_ReturnsProfile_FromClient_ForPasswordLoginPrincipal()
    {
        var profile = new UserSummaryDto(
            Guid.NewGuid(),
            "user@example.com",
            "Example User",
            true,
            false,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            [Guid.NewGuid()],
            [],
            []);

        var client = new TrackingEcmApiClient(profile);
        var controller = new IamUserProfileController(client);

        var httpContext = new DefaultHttpContext
        {
            User = CreatePasswordLoginPrincipal(profile)
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.GetAsync(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProfile = okResult.Value.Should().BeOfType<UserSummaryDto>().Subject;
        returnedProfile.Should().BeEquivalentTo(profile);
    }

    private static ClaimsPrincipal CreatePasswordLoginPrincipal(UserSummaryDto profile)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, profile.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, profile.DisplayName));
        identity.AddClaim(new Claim(ClaimTypes.Email, profile.Email));
        identity.AddClaim(new Claim("preferred_username", profile.Email));
        identity.AddClaim(new Claim(PasswordLoginClaims.MarkerClaimType, PasswordLoginClaims.MarkerClaimValue));
        identity.AddClaim(new Claim(PasswordLoginClaims.ProfileClaimType, JsonSerializer.Serialize(profile)));

        return new ClaimsPrincipal(identity);
    }

    private sealed class TrackingEcmApiClient(UserSummaryDto profile) : IUsersApiClient
    {
        public Task<IReadOnlyCollection<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<UserSummaryDto>>([]);

        public Task<UserSummaryDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<UserSummaryDto?>(null);

        public Task<UserSummaryDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult<UserSummaryDto?>(null);

        public Task<UserSummaryDto?> GetCurrentUserProfileAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<UserSummaryDto?>(profile);

        public Task<UserSummaryDto?> AuthenticateUserAsync(AuthenticateUserRequestDto requestDto, CancellationToken cancellationToken = default)
            => Task.FromResult<UserSummaryDto?>(null);

        public Task<UserSummaryDto?> CreateUserAsync(CreateUserRequestDto requestDto, CancellationToken cancellationToken = default)
            => Task.FromResult<UserSummaryDto?>(null);

        public Task<UserSummaryDto?> UpdateUserAsync(Guid userId, UpdateUserRequestDto requestDto, CancellationToken cancellationToken = default)
            => Task.FromResult<UserSummaryDto?>(null);

        public Task<UserSummaryDto?> UpdateCurrentUserProfileAsync(UpdateUserProfileRequestDto requestDto, CancellationToken cancellationToken = default)
            => Task.FromResult<UserSummaryDto?>(profile);

        public Task<PasswordUpdateResult> UpdateCurrentUserPasswordAsync(UpdateUserPasswordRequestDto requestDto, CancellationToken cancellationToken = default)
            => Task.FromResult(new PasswordUpdateResult(HttpStatusCode.NoContent));

        public Task<UserSummaryDto?> AssignRoleToUserAsync(Guid userId, AssignRoleRequestDto requestDto, CancellationToken cancellationToken = default)
            => Task.FromResult<UserSummaryDto?>(null);

        public Task<UserSummaryDto?> RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
            => Task.FromResult<UserSummaryDto?>(null);
    }
}
