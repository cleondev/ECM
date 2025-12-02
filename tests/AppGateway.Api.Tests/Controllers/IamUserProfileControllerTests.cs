using System.Security.Claims;
using System.Text.Json;

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

    private sealed class TrackingEcmApiClient(UserSummaryDto profile) : IEcmApiClient
    {
        private readonly UserSummaryDto profile = profile;

        Task<IReadOnlyCollection<UserSummaryDto>> IEcmApiClient.GetUsersAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<UserSummaryDto?> IEcmApiClient.GetUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<UserSummaryDto?> IEcmApiClient.GetUserByEmailAsync(string email, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<UserSummaryDto?> IEcmApiClient.GetCurrentUserProfileAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<UserSummaryDto?> IEcmApiClient.AuthenticateUserAsync(AuthenticateUserRequestDto requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<UserSummaryDto?> IEcmApiClient.CreateUserAsync(CreateUserRequestDto requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<UserSummaryDto?> IEcmApiClient.UpdateUserAsync(Guid userId, UpdateUserRequestDto requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<UserSummaryDto?> IEcmApiClient.UpdateCurrentUserProfileAsync(UpdateUserProfileRequestDto requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<PasswordUpdateResult> IEcmApiClient.UpdateCurrentUserPasswordAsync(UpdateUserPasswordRequestDto requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<UserSummaryDto?> IEcmApiClient.AssignRoleToUserAsync(Guid userId, AssignRoleRequestDto requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<UserSummaryDto?> IEcmApiClient.RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<IReadOnlyCollection<RoleSummaryDto>> IEcmApiClient.GetRolesAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<RoleSummaryDto?> IEcmApiClient.CreateRoleAsync(CreateRoleRequestDto requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<RoleSummaryDto?> IEcmApiClient.RenameRoleAsync(Guid roleId, RenameRoleRequestDto requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<bool> IEcmApiClient.DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<IReadOnlyCollection<AccessRelationDto>> IEcmApiClient.GetRelationsBySubjectAsync(string subjectType, Guid subjectId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<IReadOnlyCollection<AccessRelationDto>> IEcmApiClient.GetRelationsByObjectAsync(string objectType, Guid objectId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<AccessRelationDto?> IEcmApiClient.CreateRelationAsync(CreateAccessRelationRequestDto requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<bool> IEcmApiClient.DeleteRelationAsync(string subjectType, Guid subjectId, string objectType, Guid objectId, string relation, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<DocumentListDto> IEcmApiClient.GetDocumentsAsync(ListDocumentsRequestDto requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<DocumentDto?> IEcmApiClient.GetDocumentAsync(Guid documentId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<DocumentDto?> IEcmApiClient.CreateDocumentAsync(CreateDocumentUpload requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<DocumentDto?> IEcmApiClient.UpdateDocumentAsync(Guid documentId, UpdateDocumentRequestDto requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<bool> IEcmApiClient.DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<bool> IEcmApiClient.DeleteDocumentByVersionAsync(Guid versionId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<Uri?> IEcmApiClient.GetDocumentVersionDownloadUriAsync(Guid versionId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<DocumentFileContent?> IEcmApiClient.GetDocumentVersionPreviewAsync(Guid versionId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<DocumentFileContent?> IEcmApiClient.GetDocumentVersionThumbnailAsync(Guid versionId, int width, int height, string? fit, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<DocumentShareLinkDto?> IEcmApiClient.CreateDocumentShareLinkAsync(CreateShareLinkRequestDto requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<IReadOnlyCollection<TagLabelDto>> IEcmApiClient.GetTagsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<TagLabelDto?> IEcmApiClient.CreateTagAsync(CreateTagRequestDto requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<TagLabelDto?> IEcmApiClient.UpdateTagAsync(Guid tagId, UpdateTagRequestDto requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<bool> IEcmApiClient.DeleteTagAsync(Guid tagId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<bool> IEcmApiClient.AssignTagToDocumentAsync(Guid documentId, AssignTagRequestDto requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<bool> IEcmApiClient.RemoveTagFromDocumentAsync(Guid documentId, Guid tagId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<WorkflowInstanceDto?> IEcmApiClient.StartWorkflowAsync(StartWorkflowRequestDto requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<SignatureReceiptDto?> IEcmApiClient.CreateSignatureRequestAsync(SignatureRequestDto requestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<DocumentTypeDto>> GetDocumentTypesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DocumentFileContent?> DownloadDocumentVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<GroupSummaryDto>> GetGroupsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<GroupSummaryDto?> CreateGroupAsync(CreateGroupRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<GroupSummaryDto?> UpdateGroupAsync(Guid groupId, UpdateGroupRequestDto requestDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
