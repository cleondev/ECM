using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Contracts.AccessControl.Relations;
using AppGateway.Contracts.AccessControl.Roles;
using AppGateway.Contracts.AccessControl.Users;
using AppGateway.Contracts.Documents;
using AppGateway.Contracts.Signatures;
using AppGateway.Contracts.Workflows;

namespace AppGateway.Infrastructure.Ecm;

public interface IEcmApiClient
{
    Task<IReadOnlyCollection<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> UpdateUserAsync(Guid userId, UpdateUserRequestDto request, CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> AssignRoleToUserAsync(Guid userId, AssignRoleRequestDto request, CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RoleSummaryDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<RoleSummaryDto?> CreateRoleAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default);

    Task<RoleSummaryDto?> RenameRoleAsync(Guid roleId, RenameRoleRequestDto request, CancellationToken cancellationToken = default);

    Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccessRelationDto>> GetRelationsBySubjectAsync(Guid subjectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccessRelationDto>> GetRelationsByObjectAsync(string objectType, Guid objectId, CancellationToken cancellationToken = default);

    Task<AccessRelationDto?> CreateRelationAsync(CreateAccessRelationRequestDto request, CancellationToken cancellationToken = default);

    Task<bool> DeleteRelationAsync(Guid subjectId, string objectType, Guid objectId, string relation, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DocumentSummaryDto>> GetDocumentsAsync(CancellationToken cancellationToken = default);

    Task<DocumentSummaryDto?> CreateDocumentAsync(CreateDocumentRequestDto request, CancellationToken cancellationToken = default);

    Task<WorkflowInstanceDto?> StartWorkflowAsync(StartWorkflowRequestDto request, CancellationToken cancellationToken = default);

    Task<SignatureReceiptDto?> CreateSignatureRequestAsync(SignatureRequestDto request, CancellationToken cancellationToken = default);
}
