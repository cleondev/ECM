using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Contracts.IAM.Groups;
using AppGateway.Contracts.IAM.Relations;
using AppGateway.Contracts.IAM.Roles;
using AppGateway.Contracts.IAM.Users;
using AppGateway.Contracts.Documents;
using AppGateway.Contracts.Tags;
using AppGateway.Contracts.Signatures;
using AppGateway.Contracts.Workflows;

namespace AppGateway.Infrastructure.Ecm;

public interface IEcmApiClient
{
    Task<IReadOnlyCollection<GroupSummaryDto>> GetGroupsAsync(CancellationToken cancellationToken = default);

    Task<GroupSummaryDto?> CreateGroupAsync(CreateGroupRequestDto requestDto, CancellationToken cancellationToken = default);

    Task<GroupSummaryDto?> UpdateGroupAsync(Guid groupId, UpdateGroupRequestDto requestDto, CancellationToken cancellationToken = default);

    Task<bool> DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> GetCurrentUserProfileAsync(CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> AuthenticateUserAsync(
        AuthenticateUserRequestDto requestDto,
        CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> CreateUserAsync(CreateUserRequestDto requestDto, CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> UpdateUserAsync(Guid userId, UpdateUserRequestDto requestDto, CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> UpdateCurrentUserProfileAsync(UpdateUserProfileRequestDto requestDto, CancellationToken cancellationToken = default);

    Task<PasswordUpdateResult> UpdateCurrentUserPasswordAsync(UpdateUserPasswordRequestDto requestDto, CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> AssignRoleToUserAsync(Guid userId, AssignRoleRequestDto requestDto, CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RoleSummaryDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<RoleSummaryDto?> CreateRoleAsync(CreateRoleRequestDto requestDto, CancellationToken cancellationToken = default);

    Task<RoleSummaryDto?> RenameRoleAsync(Guid roleId, RenameRoleRequestDto requestDto, CancellationToken cancellationToken = default);

    Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccessRelationDto>> GetRelationsBySubjectAsync(string subjectType, Guid subjectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccessRelationDto>> GetRelationsByObjectAsync(string objectType, Guid objectId, CancellationToken cancellationToken = default);

    Task<AccessRelationDto?> CreateRelationAsync(CreateAccessRelationRequestDto requestDto, CancellationToken cancellationToken = default);

    Task<bool> DeleteRelationAsync(string subjectType, Guid subjectId, string objectType, Guid objectId, string relation, CancellationToken cancellationToken = default);

    Task<DocumentListDto> GetDocumentsAsync(ListDocumentsRequestDto requestDto, CancellationToken cancellationToken = default);

    Task<DocumentDto?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<DocumentDto?> CreateDocumentAsync(CreateDocumentUpload requestDto, CancellationToken cancellationToken = default);

    Task<DocumentDto?> UpdateDocumentAsync(
        Guid documentId,
        UpdateDocumentRequestDto requestDto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<bool> DeleteDocumentByVersionAsync(Guid versionId, CancellationToken cancellationToken = default);

    Task<Uri?> GetDocumentVersionDownloadUriAsync(Guid versionId, CancellationToken cancellationToken = default);

    Task<DocumentFileContent?> DownloadDocumentVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DocumentTypeDto>> GetDocumentTypesAsync(CancellationToken cancellationToken = default);

    Task<DocumentFileContent?> GetDocumentVersionPreviewAsync(Guid versionId, CancellationToken cancellationToken = default);

    Task<DocumentFileContent?> GetDocumentVersionThumbnailAsync(
        Guid versionId,
        int width,
        int height,
        string? fit,
        CancellationToken cancellationToken = default);

    Task<DocumentShareLinkDto?> CreateDocumentShareLinkAsync(
        CreateShareLinkRequestDto requestDto,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TagLabelDto>> GetTagsAsync(CancellationToken cancellationToken = default);

    Task<TagLabelDto?> CreateTagAsync(CreateTagRequestDto requestDto, CancellationToken cancellationToken = default);

    Task<TagLabelDto?> UpdateTagAsync(Guid tagId, UpdateTagRequestDto requestDto, CancellationToken cancellationToken = default);

    Task<bool> DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default);

    Task<bool> AssignTagToDocumentAsync(Guid documentId, AssignTagRequestDto requestDto, CancellationToken cancellationToken = default);

    Task<bool> RemoveTagFromDocumentAsync(Guid documentId, Guid tagId, CancellationToken cancellationToken = default);

    Task<WorkflowInstanceDto?> StartWorkflowAsync(StartWorkflowRequestDto requestDto, CancellationToken cancellationToken = default);

    Task<SignatureReceiptDto?> CreateSignatureRequestAsync(SignatureRequestDto requestDto, CancellationToken cancellationToken = default);
}

public sealed record PasswordUpdateResult(HttpStatusCode StatusCode, string? Content = null, string? ContentType = null)
{
    public bool IsSuccess => StatusCode == HttpStatusCode.NoContent;

    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
}
