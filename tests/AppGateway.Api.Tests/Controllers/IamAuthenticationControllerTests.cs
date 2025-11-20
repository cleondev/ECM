using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Api.Auth;
using AppGateway.Api.Controllers.IAM;
using AppGateway.Contracts.Documents;
using AppGateway.Contracts.IAM.Groups;
using AppGateway.Contracts.IAM.Relations;
using AppGateway.Contracts.IAM.Roles;
using AppGateway.Contracts.IAM.Users;
using AppGateway.Contracts.Signatures;
using AppGateway.Contracts.Tags;
using AppGateway.Contracts.Workflows;
using AppGateway.Infrastructure.Auth;
using AppGateway.Infrastructure.Ecm;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AppGateway.Api.Tests.Controllers;

public class IamAuthenticationControllerTests
{
    [Fact]
    public async Task CheckLoginAsync_ReturnsLocalProfileWithoutUpstreamCall()
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

        var client = new TrackingEcmApiClient();
        var provisioningService = new TrackingProvisioningService();
        var controller = new IamAuthenticationController(
            client,
            provisioningService,
            new TestOptionsSnapshot<CookieAuthenticationOptions>(new CookieAuthenticationOptions()),
            NullLogger<IamAuthenticationController>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost", 5090);
        httpContext.User = CreatePasswordLoginPrincipal(profile);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.CheckLoginAsync(null, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CheckLoginResponseDto>().Subject;

        response.IsAuthenticated.Should().BeTrue();
        response.Profile.Should().BeEquivalentTo(profile);
        provisioningService.CallCount.Should().Be(0);
        client.GetCurrentUserProfileCalls.Should().Be(0);
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

    [Fact]
    public async Task SignInOnBehalfAsync_SignsInWithOnBehalfClaimAndReturnsExpiry()
    {
        var profile = new UserSummaryDto(
            Guid.NewGuid(),
            "impersonated@example.com",
            "Impersonated User",
            true,
            false,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            [Guid.NewGuid()],
            [],
            []);

        var client = new TrackingEcmApiClient
        {
            UserToReturn = profile
        };

        var cookieOptions = new CookieAuthenticationOptions
        {
            ExpireTimeSpan = TimeSpan.FromHours(2)
        };

        var controller = new IamAuthenticationController(
            client,
            new TrackingProvisioningService(),
            new TestOptionsSnapshot<CookieAuthenticationOptions>(cookieOptions),
            NullLogger<IamAuthenticationController>.Instance);

        var authService = new RecordingAuthenticationService();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton<IAuthenticationService>(authService)
                .BuildServiceProvider(),
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.SignInOnBehalfAsync(
            new OnBehalfLoginRequest { UserEmail = profile.Email },
            CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<OnBehalfLoginResponseDto>().Subject;

        response.User.Should().BeEquivalentTo(profile);
        response.ExpiresOn.Should().BeCloseTo(
            DateTimeOffset.UtcNow.Add(cookieOptions.ExpireTimeSpan!.Value),
            TimeSpan.FromSeconds(5));

        authService.SignInSchemes.Should().Contain(CookieAuthenticationDefaults.AuthenticationScheme);
        authService.SignOutSchemes.Should().Contain(CookieAuthenticationDefaults.AuthenticationScheme);

        var identity = authService.LastSignInPrincipal.Should().NotBeNull().Subject.Identity.Should()
            .BeAssignableTo<ClaimsIdentity>().Subject;

        identity.Claims.Should().Contain(c =>
            c.Type == PasswordLoginClaims.OnBehalfClaimType && c.Value == "true");
        identity.Claims.Should().Contain(c =>
            c.Type == PasswordLoginClaims.ProfileClaimType && c.Value == JsonSerializer.Serialize(profile));
    }

    [Fact]
    public async Task SignInOnBehalfAsync_ReturnsBadRequestWhenNoIdentifierProvided()
    {
        var controller = new IamAuthenticationController(
            new TrackingEcmApiClient(),
            new TrackingProvisioningService(),
            new TestOptionsSnapshot<CookieAuthenticationOptions>(new CookieAuthenticationOptions()),
            NullLogger<IamAuthenticationController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.SignInOnBehalfAsync(new OnBehalfLoginRequest(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SignInOnBehalfAsync_ReturnsNotFoundWhenUserMissing()
    {
        var client = new TrackingEcmApiClient();

        var controller = new IamAuthenticationController(
            client,
            new TrackingProvisioningService(),
            new TestOptionsSnapshot<CookieAuthenticationOptions>(new CookieAuthenticationOptions()),
            NullLogger<IamAuthenticationController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.SignInOnBehalfAsync(
            new OnBehalfLoginRequest { UserEmail = "missing@example.com" },
            CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
        client.GetUserByEmailCalls.Should().Be(1);
    }

    private sealed class TrackingProvisioningService : IUserProvisioningService
    {
        public int CallCount { get; private set; }

        public Task<UserSummaryDto?> EnsureUserExistsAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult<UserSummaryDto?>(null);
        }
    }

    private sealed class TrackingEcmApiClient : IEcmApiClient
    {
        public int GetCurrentUserProfileCalls { get; private set; }

        public UserSummaryDto? UserToReturn { get; init; }

        public int GetUserByEmailCalls { get; private set; }

        public Task<UserSummaryDto?> GetCurrentUserProfileAsync(CancellationToken cancellationToken = default)
        {
            GetCurrentUserProfileCalls++;
            return Task.FromResult<UserSummaryDto?>(null);
        }

        public Task<UserSummaryDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UserToReturn);
        }

        public Task<UserSummaryDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            GetUserByEmailCalls++;
            return Task.FromResult(UserToReturn);
        }

        public Task<IReadOnlyCollection<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserSummaryDto?> AuthenticateUserAsync(
            AuthenticateUserRequestDto request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserSummaryDto?> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserSummaryDto?> UpdateUserAsync(Guid userId, UpdateUserRequestDto request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserSummaryDto?> UpdateCurrentUserProfileAsync(UpdateUserProfileRequestDto request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PasswordUpdateResult> UpdateCurrentUserPasswordAsync(
            UpdateUserPasswordRequestDto request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserSummaryDto?> AssignRoleToUserAsync(Guid userId, AssignRoleRequestDto request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserSummaryDto?> RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<AppGateway.Contracts.IAM.Roles.RoleSummaryDto>> GetRolesAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AppGateway.Contracts.IAM.Roles.RoleSummaryDto?> CreateRoleAsync(
            CreateRoleRequestDto request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AppGateway.Contracts.IAM.Roles.RoleSummaryDto?> RenameRoleAsync(
            Guid roleId,
            RenameRoleRequestDto request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<AppGateway.Contracts.IAM.Relations.AccessRelationDto>> GetRelationsBySubjectAsync(
            string subjectType,
            Guid subjectId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<AppGateway.Contracts.IAM.Relations.AccessRelationDto>> GetRelationsByObjectAsync(
            string objectType,
            Guid objectId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AppGateway.Contracts.IAM.Relations.AccessRelationDto?> CreateRelationAsync(
            CreateAccessRelationRequestDto request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteRelationAsync(
            string subjectType,
            Guid subjectId,
            string objectType,
            Guid objectId,
            string relation,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AppGateway.Contracts.Documents.DocumentListDto> GetDocumentsAsync(
            ListDocumentsRequestDto request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AppGateway.Contracts.Documents.DocumentDto?> CreateDocumentAsync(
            CreateDocumentUpload request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentDto?> UpdateDocumentAsync(
            Guid documentId,
            UpdateDocumentRequestDto request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Uri?> GetDocumentVersionDownloadUriAsync(Guid versionId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentFileContent?> GetDocumentVersionPreviewAsync(
            Guid versionId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentFileContent?> GetDocumentVersionThumbnailAsync(
            Guid versionId,
            int width,
            int height,
            string? fit,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentShareLinkDto?> CreateDocumentShareLinkAsync(
            CreateShareLinkRequestDto request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<AppGateway.Contracts.Tags.TagLabelDto>> GetTagsAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AppGateway.Contracts.Tags.TagLabelDto?> CreateTagAsync(
            CreateTagRequestDto request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AppGateway.Contracts.Tags.TagLabelDto?> UpdateTagAsync(
            Guid tagId,
            UpdateTagRequestDto request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> AssignTagToDocumentAsync(
            Guid documentId,
            AssignTagRequestDto request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> RemoveTagFromDocumentAsync(Guid documentId, Guid tagId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AppGateway.Contracts.Workflows.WorkflowInstanceDto?> StartWorkflowAsync(
            StartWorkflowRequestDto request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AppGateway.Contracts.Signatures.SignatureReceiptDto?> CreateSignatureRequestAsync(
            SignatureRequestDto request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class RecordingAuthenticationService : IAuthenticationService
    {
        public ClaimsPrincipal? LastSignInPrincipal { get; private set; }

        public List<string> SignInSchemes { get; } = new();

        public List<string> SignOutSchemes { get; } = new();

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            LastSignInPrincipal = principal;
            if (scheme is not null)
            {
                SignInSchemes.Add(scheme);
            }

            context.User = principal;
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            if (scheme is not null)
            {
                SignOutSchemes.Add(scheme);
            }

            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            return Task.CompletedTask;
        }
    }

    private sealed class TestOptionsSnapshot<T>(T value) : IOptionsSnapshot<T>
        where T : class
    {
        private readonly T _value = value;

        public T Value => _value;

        public T Get(string? name) => _value;
    }
}
