using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Contracts.IAM.Relations;
using AppGateway.Contracts.IAM.Roles;
using AppGateway.Contracts.IAM.Users;
using AppGateway.Contracts.Documents;
using AppGateway.Contracts.Signatures;
using AppGateway.Contracts.Workflows;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace AppGateway.Infrastructure.Ecm;

internal sealed class EcmApiClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    ITokenAcquisition tokenAcquisition,
    IOptions<EcmApiClientOptions> options,
    ILogger<EcmApiClient> logger) : IEcmApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ITokenAcquisition _tokenAcquisition = tokenAcquisition;
    private readonly EcmApiClientOptions _options = options.Value;
    private readonly ILogger<EcmApiClient> _logger = logger;

    public async Task<IReadOnlyCollection<UserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, "api/iam/users", cancellationToken);
        var response = await SendAsync<IReadOnlyCollection<UserSummaryDto>>(request, cancellationToken);
        return response ?? [];
    }

    public async Task<UserSummaryDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"api/iam/users/{userId}", cancellationToken);
        return await SendAsync<UserSummaryDto>(request, cancellationToken);
    }

    public async Task<UserSummaryDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var uri = QueryHelpers.AddQueryString("api/iam/users/by-email", "email", email);
        using var request = await CreateRequestAsync(HttpMethod.Get, uri, cancellationToken);
        return await SendAsync<UserSummaryDto>(request, cancellationToken);
    }

    public async Task<UserSummaryDto?> GetCurrentUserProfileAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, "api/iam/profile", cancellationToken);
        return await SendAsync<UserSummaryDto>(request, cancellationToken);
    }

    public async Task<UserSummaryDto?> CreateUserAsync(CreateUserRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, "api/iam/users", cancellationToken);
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<UserSummaryDto>(request, cancellationToken);
    }

    public async Task<UserSummaryDto?> UpdateUserAsync(Guid userId, UpdateUserRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Put, $"api/iam/users/{userId}", cancellationToken);
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<UserSummaryDto>(request, cancellationToken);
    }

    public async Task<UserSummaryDto?> UpdateCurrentUserProfileAsync(UpdateUserProfileRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Put, "api/iam/profile", cancellationToken);
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<UserSummaryDto>(request, cancellationToken);
    }

    public async Task<UserSummaryDto?> AssignRoleToUserAsync(Guid userId, AssignRoleRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, $"api/iam/users/{userId}/roles", cancellationToken);
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<UserSummaryDto>(request, cancellationToken);
    }

    public async Task<UserSummaryDto?> RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Delete, $"api/iam/users/{userId}/roles/{roleId}", cancellationToken);
        return await SendAsync<UserSummaryDto>(request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<RoleSummaryDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, "api/iam/roles", cancellationToken);
        var response = await SendAsync<IReadOnlyCollection<RoleSummaryDto>>(request, cancellationToken);
        return response ?? [];
    }

    public async Task<RoleSummaryDto?> CreateRoleAsync(CreateRoleRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, "api/iam/roles", cancellationToken);
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<RoleSummaryDto>(request, cancellationToken);
    }

    public async Task<RoleSummaryDto?> RenameRoleAsync(Guid roleId, RenameRoleRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Put, $"api/iam/roles/{roleId}", cancellationToken);
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<RoleSummaryDto>(request, cancellationToken);
    }

    public async Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Delete, $"api/iam/roles/{roleId}", cancellationToken);
        return await SendAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AccessRelationDto>> GetRelationsBySubjectAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"api/iam/relations/subjects/{subjectId}", cancellationToken);
        var response = await SendAsync<IReadOnlyCollection<AccessRelationDto>>(request, cancellationToken);
        return response ?? [];
    }

    public async Task<IReadOnlyCollection<AccessRelationDto>> GetRelationsByObjectAsync(string objectType, Guid objectId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"api/iam/relations/objects/{Uri.EscapeDataString(objectType)}/{objectId}", cancellationToken);
        var response = await SendAsync<IReadOnlyCollection<AccessRelationDto>>(request, cancellationToken);
        return response ?? [];
    }

    public async Task<AccessRelationDto?> CreateRelationAsync(CreateAccessRelationRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, "api/iam/relations", cancellationToken);
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<AccessRelationDto>(request, cancellationToken);
    }

    public async Task<bool> DeleteRelationAsync(Guid subjectId, string objectType, Guid objectId, string relation, CancellationToken cancellationToken = default)
    {
        var uri = $"api/iam/relations/subjects/{subjectId}/objects/{Uri.EscapeDataString(objectType)}/{objectId}?relation={Uri.EscapeDataString(relation)}";
        using var request = await CreateRequestAsync(HttpMethod.Delete, uri, cancellationToken);
        return await SendAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DocumentSummaryDto>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, "api/ecm/documents", cancellationToken);
        var response = await SendAsync<IReadOnlyCollection<DocumentSummaryDto>>(request, cancellationToken);
        return response ?? [];
    }

    public async Task<DocumentSummaryDto?> CreateDocumentAsync(CreateDocumentRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, "api/ecm/documents", cancellationToken);
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<DocumentSummaryDto>(request, cancellationToken);
    }

    public async Task<WorkflowInstanceDto?> StartWorkflowAsync(StartWorkflowRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, "api/ecm/workflows/instances", cancellationToken);
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<WorkflowInstanceDto>(request, cancellationToken);
    }

    public async Task<SignatureReceiptDto?> CreateSignatureRequestAsync(SignatureRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, "api/ecm/signatures", cancellationToken);
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<SignatureReceiptDto>(request, cancellationToken);
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string uri, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, uri);
        await AttachAuthenticationAsync(request, cancellationToken);

        return request;
    }

    private async Task AttachAuthenticationAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization;
        if (!string.IsNullOrWhiteSpace(authorization))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorization.ToString());
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Scope))
        {
            return;
        }

        var scopes = ParseScopes(_options.Scope);
        if (scopes.Length == 0)
        {
            return;
        }

        var tokenAcquisitionOptions = new TokenAcquisitionOptions
        {
            CancellationToken = cancellationToken,
        };

        var authenticationScheme = string.IsNullOrWhiteSpace(_options.AuthenticationScheme)
            ? OpenIdConnectDefaults.AuthenticationScheme
            : _options.AuthenticationScheme;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            try
            {
                var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(
                    scopes,
                    authenticationScheme: authenticationScheme,
                    tenantId: _options.TenantId,
                    user: httpContext.User,
                    tokenAcquisitionOptions: tokenAcquisitionOptions);

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                return;
            }
            catch (MsalException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Falling back to app-only token while calling ECM API because acquiring a user token failed.");
            }
        }

        var appToken = await _tokenAcquisition.GetAccessTokenForAppAsync(
            _options.Scope!,
            authenticationScheme: authenticationScheme,
            tenant: _options.TenantId,
            tokenAcquisitionOptions: tokenAcquisitionOptions);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", appToken);
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

    private static string[] ParseScopes(string scope)
        => scope
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
