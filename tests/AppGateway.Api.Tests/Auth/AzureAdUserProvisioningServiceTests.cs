using System;
using System.Security.Claims;
using AppGateway.Api.Auth;
using AppGateway.Contracts.Documents;
using AppGateway.Contracts.IAM.Groups;
using AppGateway.Contracts.IAM.Relations;
using AppGateway.Contracts.IAM.Roles;
using AppGateway.Contracts.IAM.Users;
using AppGateway.Contracts.Signatures;
using AppGateway.Contracts.Workflows;
using AppGateway.Contracts.Tags;
using AppGateway.Infrastructure.Ecm;
using AppGateway.Infrastructure.IAM;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AppGateway.Api.Tests.Auth;

public class AzureAdUserProvisioningServiceTests
{
    [Fact]
    public async Task EnsureUserExistsAsync_ReturnsNull_WhenPrincipalIsNull()
    {
        var service = CreateService();

        var result = await service.EnsureUserExistsAsync(null, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task EnsureUserExistsAsync_ReturnsNull_WhenEmailClaimMissing()
    {
        var service = CreateService();
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        var result = await service.EnsureUserExistsAsync(principal, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task EnsureUserExistsAsync_ReturnsExistingUser_WhenUserAlreadyProvisioned()
    {
        var existingUser = CreateUserSummary();
        var client = new FakeEcmApiClient
        {
            ExistingUser = existingUser
        };
        var service = CreateService(client: client);
        var principal = CreatePrincipal(existingUser.Email, existingUser.DisplayName);

        var result = await service.EnsureUserExistsAsync(principal, CancellationToken.None);

        result.Should().Be(existingUser);
        client.CreateUserCalls.Should().Be(0);
    }

    [Fact]
    public async Task EnsureUserExistsAsync_CreatesUser_WhenUserDoesNotExist()
    {
        var createdUser = CreateUserSummary();
        var role = new RoleSummaryDto(Guid.NewGuid(), "Default");
        var client = new FakeEcmApiClient
        {
            CreatedUser = createdUser,
            Roles = [role]
        };

        var options = Options.Create(new IamOptions
        {
            DefaultRoleName = role.Name
        });

        var service = CreateService(client: client, options: options);
        var principal = CreatePrincipal(createdUser.Email, createdUser.DisplayName);

        var result = await service.EnsureUserExistsAsync(principal, CancellationToken.None);

        result.Should().Be(createdUser);
        client.CreateUserCalls.Should().Be(1);
        client.LastCreateRequest.Should().NotBeNull();
        client.LastCreateRequest!.RoleIds.Should().ContainSingle().Which.Should().Be(role.Id);
        client.LastCreateRequest!.GroupIds
            .Should()
            .BeEquivalentTo(
            [
                GroupDefaultIds.System,
                GroupDefaultIds.Guest
            ]);
        client.LastCreateRequest!.PrimaryGroupId.Should().BeNull();
    }

    [Fact]
    public async Task EnsureUserExistsAsync_IncludesClaimedGroups_WhenPresent()
    {
        var createdUser = CreateUserSummary();
        var client = new FakeEcmApiClient
        {
            CreatedUser = createdUser
        };

        var service = CreateService(client: client);
        var primaryGroupId = Guid.NewGuid();
        var additionalGroupId = Guid.NewGuid();
        var principal = CreatePrincipal(
            createdUser.Email,
            createdUser.DisplayName,
            primaryGroupId: primaryGroupId,
            groupIds: [additionalGroupId]);

        var result = await service.EnsureUserExistsAsync(principal, CancellationToken.None);

        result.Should().Be(createdUser);
        client.LastCreateRequest.Should().NotBeNull();
        client.LastCreateRequest!.GroupIds
            .Should()
            .Contain([GroupDefaultIds.System, GroupDefaultIds.Guest, additionalGroupId, primaryGroupId]);
        client.LastCreateRequest!.PrimaryGroupId.Should().Be(primaryGroupId);
    }

    [Fact]
    public async Task EnsureUserExistsAsync_UsesAlternativeEmailClaim_WhenEmailClaimPresent()
    {
        var existingUser = CreateUserSummary();
        var client = new FakeEcmApiClient
        {
            ExistingUser = existingUser
        };
        var service = CreateService(client: client);
        var principal = CreatePrincipal(existingUser.Email, existingUser.DisplayName, claimType: "email");

        var result = await service.EnsureUserExistsAsync(principal, CancellationToken.None);

        result.Should().Be(existingUser);
    }

    [Fact]
    public async Task EnsureUserExistsAsync_UsesEmailsArrayClaim_WhenPresent()
    {
        var existingUser = CreateUserSummary();
        var client = new FakeEcmApiClient
        {
            ExistingUser = existingUser
        };
        var service = CreateService(client: client);
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("emails", $"[\"{existingUser.Email}\"]"));
        identity.AddClaim(new Claim("name", existingUser.DisplayName));
        var principal = new ClaimsPrincipal(identity);

        var result = await service.EnsureUserExistsAsync(principal, CancellationToken.None);

        result.Should().Be(existingUser);
    }

    private static AzureAdUserProvisioningService CreateService(
        FakeEcmApiClient? client = null,
        IOptions<IamOptions>? options = null)
    {
        client ??= new FakeEcmApiClient();
        options ??= Options.Create(new IamOptions());

        return new AzureAdUserProvisioningService(
            client,
            options,
            NullLogger<AzureAdUserProvisioningService>.Instance);
    }

    private static ClaimsPrincipal CreatePrincipal(
        string email,
        string name,
        string claimType = ClaimTypes.Email,
        Guid? primaryGroupId = null,
        Guid[]? groupIds = null)
    {
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(claimType, email));
        identity.AddClaim(new Claim("name", name));
        if (primaryGroupId.HasValue)
        {
            identity.AddClaim(new Claim("primary_group_id", primaryGroupId.Value.ToString()));
            identity.AddClaim(new Claim("group_id", primaryGroupId.Value.ToString()));
        }

        if (groupIds is not null)
        {
            foreach (var groupId in groupIds)
            {
                identity.AddClaim(new Claim("group_id", groupId.ToString()));
            }
        }
        return new ClaimsPrincipal(identity);
    }

    private static UserSummaryDto CreateUserSummary()
        => new(Guid.NewGuid(), "user@example.com", "User", true, false, DateTimeOffset.UtcNow, null, [], [], []);

    private sealed class FakeEcmApiClient : IEcmApiClient
    {
        public UserSummaryDto? ExistingUser { get; set; }

        public UserSummaryDto? CreatedUser { get; set; }

        public IReadOnlyCollection<RoleSummaryDto> Roles { get; set; } = [];

        public CreateUserRequestDto? LastCreateRequest { get; private set; }

        public int CreateUserCalls { get; private set; }

        public Task<UserSummaryDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(ExistingUser);

        public Task<UserSummaryDto?> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default)
        {
            CreateUserCalls++;
            LastCreateRequest = request;
            return Task.FromResult(CreatedUser);
        }

        public Task<IReadOnlyCollection<RoleSummaryDto>> GetRolesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Roles);

        #region Unused members

        public Task<IReadOnlyCollection<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserSummaryDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserSummaryDto?> GetCurrentUserProfileAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserSummaryDto?> AuthenticateUserAsync(
            AuthenticateUserRequestDto request,
            CancellationToken cancellationToken = default)
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

        public Task<TagLabelDto?> UpdateTagAsync(
            Guid tagId,
            UpdateTagRequestDto request,
            CancellationToken cancellationToken = default)
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

        #endregion
    }
}
