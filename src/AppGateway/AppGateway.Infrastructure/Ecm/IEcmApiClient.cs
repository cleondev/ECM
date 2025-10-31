using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
    Task<IReadOnlyCollection<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> GetCurrentUserProfileAsync(CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> AuthenticateUserAsync(
        AuthenticateUserRequestDto request,
        CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> UpdateUserAsync(Guid userId, UpdateUserRequestDto request, CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> UpdateCurrentUserProfileAsync(UpdateUserProfileRequestDto request, CancellationToken cancellationToken = default);

    Task<PasswordUpdateResult> UpdateCurrentUserPasswordAsync(UpdateUserPasswordRequestDto request, CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> AssignRoleToUserAsync(Guid userId, AssignRoleRequestDto request, CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RoleSummaryDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<RoleSummaryDto?> CreateRoleAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default);

    Task<RoleSummaryDto?> RenameRoleAsync(Guid roleId, RenameRoleRequestDto request, CancellationToken cancellationToken = default);

    Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccessRelationDto>> GetRelationsBySubjectAsync(string subjectType, Guid subjectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccessRelationDto>> GetRelationsByObjectAsync(string objectType, Guid objectId, CancellationToken cancellationToken = default);

    Task<AccessRelationDto?> CreateRelationAsync(CreateAccessRelationRequestDto request, CancellationToken cancellationToken = default);

    Task<bool> DeleteRelationAsync(string subjectType, Guid subjectId, string objectType, Guid objectId, string relation, CancellationToken cancellationToken = default);

    Task<DocumentListDto> GetDocumentsAsync(ListDocumentsRequestDto request, CancellationToken cancellationToken = default);

    Task<DocumentDto?> CreateDocumentAsync(CreateDocumentUpload request, CancellationToken cancellationToken = default);

    Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<Uri?> GetDocumentVersionDownloadUriAsync(Guid versionId, CancellationToken cancellationToken = default);

    Task<DocumentFileContent?> GetDocumentVersionPreviewAsync(Guid versionId, CancellationToken cancellationToken = default);

    Task<DocumentFileContent?> GetDocumentVersionThumbnailAsync(
        Guid versionId,
        int width,
        int height,
        string? fit,
        CancellationToken cancellationToken = default);

    Task<DocumentShareLinkDto?> CreateDocumentShareLinkAsync(
        CreateShareLinkRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TagLabelDto>> GetTagsAsync(CancellationToken cancellationToken = default);

    Task<TagLabelDto?> CreateTagAsync(CreateTagRequestDto request, CancellationToken cancellationToken = default);

    Task<TagLabelDto?> UpdateTagAsync(Guid tagId, UpdateTagRequestDto request, CancellationToken cancellationToken = default);

    Task<bool> DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default);

    Task<bool> AssignTagToDocumentAsync(Guid documentId, AssignTagRequestDto request, CancellationToken cancellationToken = default);

    Task<bool> RemoveTagFromDocumentAsync(Guid documentId, Guid tagId, CancellationToken cancellationToken = default);

    Task<WorkflowInstanceDto?> StartWorkflowAsync(StartWorkflowRequestDto request, CancellationToken cancellationToken = default);

    Task<SignatureReceiptDto?> CreateSignatureRequestAsync(SignatureRequestDto request, CancellationToken cancellationToken = default);
}

public sealed record PasswordUpdateResult(HttpStatusCode StatusCode, string? Content = null, string? ContentType = null)
{
    public bool IsSuccess => StatusCode == HttpStatusCode.NoContent;

    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
}
