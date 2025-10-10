using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Contracts.AccessControl.Relations;
using AppGateway.Contracts.AccessControl.Roles;
using AppGateway.Contracts.AccessControl.Users;
using AppGateway.Contracts.Documents;
using AppGateway.Contracts.Signatures;
using AppGateway.Contracts.Workflows;
using Microsoft.AspNetCore.Http;

namespace AppGateway.Infrastructure.Ecm;

internal sealed class EcmApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor) : IEcmApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task<IReadOnlyCollection<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "api/access-control/users");
        var response = await SendAsync<IReadOnlyCollection<UserSummaryDto>>(request, cancellationToken);
        return response ?? [];
    }

    public async Task<UserSummaryDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/access-control/users/{userId}");
        return await SendAsync<UserSummaryDto>(request, cancellationToken);
    }

    public async Task<UserSummaryDto?> CreateUserAsync(CreateUserRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, "api/access-control/users");
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<UserSummaryDto>(request, cancellationToken);
    }

    public async Task<UserSummaryDto?> UpdateUserAsync(Guid userId, UpdateUserRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Put, $"api/access-control/users/{userId}");
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<UserSummaryDto>(request, cancellationToken);
    }

    public async Task<UserSummaryDto?> AssignRoleToUserAsync(Guid userId, AssignRoleRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"api/access-control/users/{userId}/roles");
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<UserSummaryDto>(request, cancellationToken);
    }

    public async Task<UserSummaryDto?> RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Delete, $"api/access-control/users/{userId}/roles/{roleId}");
        return await SendAsync<UserSummaryDto>(request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<RoleSummaryDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "api/access-control/roles");
        var response = await SendAsync<IReadOnlyCollection<RoleSummaryDto>>(request, cancellationToken);
        return response ?? [];
    }

    public async Task<RoleSummaryDto?> CreateRoleAsync(CreateRoleRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, "api/access-control/roles");
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<RoleSummaryDto>(request, cancellationToken);
    }

    public async Task<RoleSummaryDto?> RenameRoleAsync(Guid roleId, RenameRoleRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Put, $"api/access-control/roles/{roleId}");
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<RoleSummaryDto>(request, cancellationToken);
    }

    public async Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Delete, $"api/access-control/roles/{roleId}");
        return await SendAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AccessRelationDto>> GetRelationsBySubjectAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/access-control/relations/subjects/{subjectId}");
        var response = await SendAsync<IReadOnlyCollection<AccessRelationDto>>(request, cancellationToken);
        return response ?? [];
    }

    public async Task<IReadOnlyCollection<AccessRelationDto>> GetRelationsByObjectAsync(string objectType, Guid objectId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"api/access-control/relations/objects/{Uri.EscapeDataString(objectType)}/{objectId}");
        var response = await SendAsync<IReadOnlyCollection<AccessRelationDto>>(request, cancellationToken);
        return response ?? [];
    }

    public async Task<AccessRelationDto?> CreateRelationAsync(CreateAccessRelationRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, "api/access-control/relations");
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<AccessRelationDto>(request, cancellationToken);
    }

    public async Task<bool> DeleteRelationAsync(Guid subjectId, string objectType, Guid objectId, string relation, CancellationToken cancellationToken = default)
    {
        var uri = $"api/access-control/relations/subjects/{subjectId}/objects/{Uri.EscapeDataString(objectType)}/{objectId}?relation={Uri.EscapeDataString(relation)}";
        using var request = CreateRequest(HttpMethod.Delete, uri);
        return await SendAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DocumentSummaryDto>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "api/ecm/documents");
        var response = await SendAsync<IReadOnlyCollection<DocumentSummaryDto>>(request, cancellationToken);
        return response ?? [];
    }

    public async Task<DocumentSummaryDto?> CreateDocumentAsync(CreateDocumentRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, "api/ecm/documents");
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<DocumentSummaryDto>(request, cancellationToken);
    }

    public async Task<WorkflowInstanceDto?> StartWorkflowAsync(StartWorkflowRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, "api/ecm/workflows/instances");
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<WorkflowInstanceDto>(request, cancellationToken);
    }

    public async Task<SignatureReceiptDto?> CreateSignatureRequestAsync(SignatureRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, "api/ecm/signatures");
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<SignatureReceiptDto>(request, cancellationToken);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string uri)
    {
        var request = new HttpRequestMessage(method, uri);
        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization;
        if (!string.IsNullOrWhiteSpace(authorization))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorization.ToString());
        }

        return request;
    }

    private async Task<T?> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    private async Task<bool> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
