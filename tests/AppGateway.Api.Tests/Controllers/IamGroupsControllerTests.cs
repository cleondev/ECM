using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AppGateway.Api.Tests.Controllers;

public class IamGroupsControllerTests
{
    [Fact]
    public async Task GetAsync_ReturnsGroups_FromClientProfile_ForPasswordLoginPrincipal()
    {
        var groups = new[]
        {
            new GroupSummaryDto(Guid.NewGuid(), "Group A"),
            new GroupSummaryDto(Guid.NewGuid(), "Group B")
        };

        var profile = new UserSummaryDto(
            Guid.NewGuid(),
            "user@example.com",
            "Example User",
            true,
            false,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            Array.Empty<Guid>(),
            Array.Empty<RoleSummaryDto>(),
            groups);

        var client = new TrackingEcmApiClient(profile);
        var controller = new IamGroupsController(client)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = CreatePasswordLoginPrincipal(profile)
                }
            }
        };

        var result = await controller.GetAsync(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGroups = okResult.Value.Should().BeAssignableTo<IReadOnlyCollection<GroupSummaryDto>>().Subject;
        returnedGroups.Should().BeEquivalentTo(groups);
        client.GetCurrentUserProfileCalls.Should().Be(1);
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

    private sealed class TrackingEcmApiClient : IEcmApiClient
    {
        private readonly UserSummaryDto? _profile;

        public TrackingEcmApiClient(UserSummaryDto? profile)
        {
            _profile = profile;
        }

        public int GetCurrentUserProfileCalls { get; private set; }

        public Task<UserSummaryDto?> GetCurrentUserProfileAsync(CancellationToken cancellationToken = default)
        {
            GetCurrentUserProfileCalls++;
            return Task.FromResult(_profile);
        }

        public Task<IReadOnlyCollection<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserSummaryDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserSummaryDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
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

        public Task<IReadOnlyCollection<RoleSummaryDto>> GetRolesAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<RoleSummaryDto?> CreateRoleAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<RoleSummaryDto?> RenameRoleAsync(Guid roleId, RenameRoleRequestDto request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<AccessRelationDto>> GetRelationsBySubjectAsync(string subjectType, Guid subjectId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<AccessRelationDto>> GetRelationsByObjectAsync(string objectType, Guid objectId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AccessRelationDto?> CreateRelationAsync(CreateAccessRelationRequestDto request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteRelationAsync(string subjectType, Guid subjectId, string objectType, Guid objectId, string relation, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentListDto> GetDocumentsAsync(ListDocumentsRequestDto request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<DocumentDto?> CreateDocumentAsync(CreateDocumentUpload request, CancellationToken cancellationToken = default)
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

        public Task<DocumentFileContent?> GetDocumentVersionPreviewAsync(Guid versionId, CancellationToken cancellationToken = default)
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

        public Task<IReadOnlyCollection<TagLabelDto>> GetTagsAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TagLabelDto?> CreateTagAsync(CreateTagRequestDto request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TagLabelDto?> UpdateTagAsync(Guid tagId, UpdateTagRequestDto request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> AssignTagToDocumentAsync(Guid documentId, AssignTagRequestDto request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> RemoveTagFromDocumentAsync(Guid documentId, Guid tagId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<WorkflowInstanceDto?> StartWorkflowAsync(StartWorkflowRequestDto request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<SignatureReceiptDto?> CreateSignatureRequestAsync(SignatureRequestDto request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
