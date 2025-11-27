using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

using AppGateway.Contracts.Documents;
using AppGateway.Contracts.IAM.Relations;
using AppGateway.Contracts.IAM.Roles;
using AppGateway.Contracts.IAM.Users;
using AppGateway.Contracts.Signatures;
using AppGateway.Contracts.Tags;
using AppGateway.Contracts.Workflows;
using AppGateway.Infrastructure.Auth;

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

using Shared.Extensions.Http;

namespace AppGateway.Infrastructure.Ecm;

internal sealed class EcmApiClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    ITokenAcquisition tokenAcquisition,
    IOptions<EcmApiClientOptions> options,
    ILogger<EcmApiClient> logger) : IEcmApiClient
{
    private const string HomeAccountIdClaimType = "homeAccountId";
    private const string ApiKeyHeaderName = "X-Api-Key";
    private const string subjectTypePublic = "public";
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
        var principal = _httpContextAccessor.HttpContext?.User;
        if (PasswordLoginClaims.IsPasswordLoginPrincipal(principal))
        {
            var profile = await ResolvePasswordLoginProfileAsync(principal, cancellationToken);
            if (profile is not null)
            {
                return profile;
            }
        }

        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            "api/iam/profile",
            cancellationToken,
            allowAppTokenFallback: false);
        return await SendAsync<UserSummaryDto>(request, cancellationToken);
    }

    public async Task<UserSummaryDto?> AuthenticateUserAsync(
        AuthenticateUserRequestDto requestDto,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Post,
            "api/iam/auth/login",
            cancellationToken,
            includeAuthentication: false);

        request.Content = JsonContent.Create(requestDto);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Authentication request failed with status {(int)response.StatusCode}: {message}");
        }

        return await response.Content.ReadFromJsonAsync<UserSummaryDto>(cancellationToken: cancellationToken);
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
        using var request = await CreateRequestAsync(
            HttpMethod.Put,
            "api/iam/profile",
            cancellationToken,
            allowAppTokenFallback: false);
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<UserSummaryDto>(request, cancellationToken);
    }

    public async Task<PasswordUpdateResult> UpdateCurrentUserPasswordAsync(UpdateUserPasswordRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Put,
            "api/iam/profile/password",
            cancellationToken,
            allowAppTokenFallback: false);
        request.Content = JsonContent.Create(requestDto);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        string? content = null;
        string? contentType = null;

        if (response.Content is not null)
        {
            contentType = response.Content.Headers.ContentType?.ToString();

            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(content))
                {
                    content = null;
                }
            }
        }

        return new PasswordUpdateResult(response.StatusCode, content, contentType);
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

    public async Task<IReadOnlyCollection<AccessRelationDto>> GetRelationsBySubjectAsync(string subjectType, Guid subjectId, CancellationToken cancellationToken = default)
    {
        var escapedSubjectType = Uri.EscapeDataString(subjectType);
        using var request = await CreateRequestAsync(HttpMethod.Get, $"api/iam/relations/subjects/{escapedSubjectType}/{subjectId}", cancellationToken);
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

    public async Task<bool> DeleteRelationAsync(string subjectType, Guid subjectId, string objectType, Guid objectId, string relation, CancellationToken cancellationToken = default)
    {
        var escapedSubjectType = Uri.EscapeDataString(subjectType);
        var uri = $"api/iam/relations/subjects/{escapedSubjectType}/{subjectId}/objects/{Uri.EscapeDataString(objectType)}/{objectId}?relation={Uri.EscapeDataString(relation)}";
        using var request = await CreateRequestAsync(HttpMethod.Delete, uri, cancellationToken);
        return await SendAsync(request, cancellationToken);
    }

    public async Task<DocumentListDto> GetDocumentsAsync(ListDocumentsRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        var uri = BuildDocumentListUri(requestDto);
        using var request = await CreateRequestAsync(HttpMethod.Get, uri, cancellationToken);
        var response = await SendAsync<DocumentListDto>(request, cancellationToken);
        return response ?? DocumentListDto.Empty;
    }

    public async Task<DocumentDto?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"api/ecm/documents/{documentId}", cancellationToken);
        return await SendAsync<DocumentDto>(request, cancellationToken);
    }

    public async Task<DocumentDto?> CreateDocumentAsync(CreateDocumentUpload requestDto, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, "api/ecm/documents", cancellationToken);

        using var form = new MultipartFormDataContent
        {
            { new StringContent(requestDto.Title), nameof(requestDto.Title) },
            { new StringContent(requestDto.DocType), nameof(requestDto.DocType) },
            { new StringContent(requestDto.Status), nameof(requestDto.Status) },
            { new StringContent(requestDto.OwnerId.ToString()), nameof(requestDto.OwnerId) },
            { new StringContent(requestDto.CreatedBy.ToString()), nameof(requestDto.CreatedBy) }
        };

        if (requestDto.GroupId.HasValue)
        {
            form.Add(new StringContent(requestDto.GroupId.Value.ToString()), nameof(requestDto.GroupId));
        }

        if (requestDto.GroupIds is { Count: > 0 })
        {
            var normalizedGroupIds = requestDto.GroupIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();

            foreach (var groupId in normalizedGroupIds)
            {
                form.Add(new StringContent(groupId.ToString()), $"{nameof(requestDto.GroupIds)}[]");
            }
        }

        if (!string.IsNullOrWhiteSpace(requestDto.Sensitivity))
        {
            form.Add(new StringContent(requestDto.Sensitivity), nameof(requestDto.Sensitivity));
        }

        if (requestDto.DocumentTypeId.HasValue)
        {
            form.Add(new StringContent(requestDto.DocumentTypeId.Value.ToString()), nameof(requestDto.DocumentTypeId));
        }

        var stream = await requestDto.OpenReadStream(cancellationToken);
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.TryParse(requestDto.ContentType, out var contentType)
            ? contentType
            : new MediaTypeHeaderValue("application/octet-stream");

        form.Add(fileContent, "File", requestDto.FileName);

        request.Content = form;
        return await SendAsync<DocumentDto>(request, cancellationToken);
    }

    public async Task<DocumentDto?> UpdateDocumentAsync(
        Guid documentId,
        UpdateDocumentRequestDto requestDto,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Put, $"api/ecm/documents/{documentId}", cancellationToken);
        request.Content = JsonContent.Create(requestDto ?? new UpdateDocumentRequestDto());
        return await SendAsync<DocumentDto>(request, cancellationToken);
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Delete, $"api/ecm/documents/{documentId}", cancellationToken);
        return await SendAsync(request, cancellationToken);
    }

    public async Task<bool> DeleteDocumentByVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Delete, $"api/ecm/files/{versionId}", cancellationToken);
        return await SendAsync(request, cancellationToken);
    }

    public async Task<Uri?> GetDocumentVersionDownloadUriAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"api/ecm/files/download/{versionId}", cancellationToken);
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        try
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (IsRedirectStatusCode(response.StatusCode))
            {
                var location = response.Headers.Location;
                if (location is null)
                {
                    return null;
                }

                if (!location.IsAbsoluteUri && _httpClient.BaseAddress is not null)
                {
                    location = new Uri(_httpClient.BaseAddress, location);
                }

                return location;
            }

            if (response.IsSuccessStatusCode)
            {
                var location = response.Headers.Location;
                if (location is not null && !location.IsAbsoluteUri && _httpClient.BaseAddress is not null)
                {
                    location = new Uri(_httpClient.BaseAddress, location);
                }

                return location;
            }

            return null;
        }
        finally
        {
            response.Dispose();
        }
    }

    public async Task<DocumentFileContent?> DownloadDocumentVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"api/ecm/files/download/{versionId}", cancellationToken);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await CreateDocumentFileContentAsync(response, enableRangeProcessing: true, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DocumentTypeDto>> GetDocumentTypesAsync(
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, "api/ecm/document-types", cancellationToken);
        var response = await SendAsync<IReadOnlyCollection<DocumentTypeDto>>(request, cancellationToken);
        return response ?? [];
    }

    public async Task<DocumentFileContent?> GetDocumentVersionPreviewAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"api/ecm/files/preview/{versionId}", cancellationToken);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await CreateDocumentFileContentAsync(response, enableRangeProcessing: true, cancellationToken);
    }

    public async Task<DocumentFileContent?> GetDocumentVersionThumbnailAsync(
        Guid versionId,
        int width,
        int height,
        string? fit,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["w"] = width.ToString(),
            ["h"] = height.ToString(),
        };

        if (!string.IsNullOrWhiteSpace(fit))
        {
            query["fit"] = fit;
        }

        var uri = QueryHelpers.AddQueryString($"api/ecm/files/thumbnails/{versionId}", query);

        using var request = await CreateRequestAsync(HttpMethod.Get, uri, cancellationToken);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await CreateDocumentFileContentAsync(response, enableRangeProcessing: false, cancellationToken);
    }

    public async Task<DocumentShareLinkDto?> CreateDocumentShareLinkAsync(
        CreateShareLinkRequestDto requestDto,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, "api/ecm/shares", cancellationToken);

        var normalizedExtension = NormalizeExtension(requestDto.FileExtension, requestDto.FileName);
        var contentType = string.IsNullOrWhiteSpace(requestDto.FileContentType)
            ? "application/octet-stream"
            : requestDto.FileContentType.Trim();

        var validFrom = DateTimeOffset.UtcNow;
        var effectiveMinutes = requestDto.GetEffectiveMinutes();
        var validTo = validFrom.AddMinutes(effectiveMinutes);

        var subjectType = requestDto.GetNormalizedSubjectType();
        var subjectId = requestDto.GetEffectiveSubjectId();

        var password = string.IsNullOrWhiteSpace(requestDto.Password)
            ? null
            : requestDto.Password.Trim();

        var payload = new
        {
            documentId = requestDto.DocumentId,
            versionId = requestDto.VersionId,
            subjectType,
            subjectId,
            permissions = new[] { "view", "download" },
            validFrom,
            validTo,
            fileName = requestDto.FileName,
            fileExtension = normalizedExtension,
            fileContentType = contentType,
            fileSizeBytes = requestDto.FileSizeBytes < 0 ? 0 : requestDto.FileSizeBytes,
            fileCreatedAt = requestDto.FileCreatedAtUtc,
            password,
        };

        request.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            if (!string.IsNullOrEmpty(password))
            {
                return null;
            }

            return await CreateLegacyShareLinkAsync(requestDto, effectiveMinutes, cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var shareResponse = await response.Content.ReadFromJsonAsync<ShareLinkResponse>(cancellationToken: cancellationToken);
        if (shareResponse is null)
        {
            return null;
        }

        var url = CreateUri(shareResponse.Url);
        var shortUrl = CreateUri(shareResponse.ShortUrl);
        var expiresAt = shareResponse.ValidTo ?? shareResponse.ValidFrom.AddMinutes(effectiveMinutes);
        var responseSubjectType = NormalizeShareSubjectType(shareResponse.SubjectType);
        var isPublic = responseSubjectType == subjectTypePublic;
        var responseSubjectId = shareResponse.SubjectId;
        if (isPublic)
        {
            responseSubjectId = null;
        }

        return new DocumentShareLinkDto(
            url,
            shortUrl,
            expiresAt,
            isPublic,
            shareResponse.RequiresPassword,
            responseSubjectType,
            responseSubjectId);
    }

    private sealed record ShareLinkResponse(
        string Url,
        string ShortUrl,
        DateTimeOffset ValidFrom,
        DateTimeOffset? ValidTo,
        JsonElement SubjectType,
        Guid? SubjectId,
        bool RequiresPassword);

    private sealed record LegacyShareLinkResponse(
        string Url,
        DateTimeOffset ExpiresAtUtc,
        bool IsPublic,
        string? ShortUrl);

    private async Task<DocumentShareLinkDto?> CreateLegacyShareLinkAsync(
        CreateShareLinkRequestDto requestDto,
        int effectiveMinutes,
        CancellationToken cancellationToken)
    {
        if (requestDto.VersionId == Guid.Empty)
        {
            return null;
        }

        var subjectType = requestDto.GetNormalizedSubjectType();
        var subjectId = requestDto.GetEffectiveSubjectId();
        var isPublic = requestDto.IsPublicShare;

        using var legacyRequest = await CreateRequestAsync(
            HttpMethod.Post,
            $"api/ecm/files/share/{requestDto.VersionId}",
            cancellationToken);

        legacyRequest.Content = JsonContent.Create(new
        {
            expiresInMinutes = effectiveMinutes,
            isPublic,
        });

        using var response = await _httpClient.SendAsync(legacyRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var legacyShare = await response.Content.ReadFromJsonAsync<LegacyShareLinkResponse>(cancellationToken: cancellationToken);
        if (legacyShare is null || string.IsNullOrWhiteSpace(legacyShare.Url))
        {
            return null;
        }

        var url = CreateUri(legacyShare.Url);
        var shortUrl = string.IsNullOrWhiteSpace(legacyShare.ShortUrl)
            ? url
            : CreateUri(legacyShare.ShortUrl);

        var legacySubjectType = legacyShare.IsPublic ? "public" : subjectType;
        var legacySubjectId = legacySubjectType == "public" ? null : subjectId;

        return new DocumentShareLinkDto(
            url,
            shortUrl,
            legacyShare.ExpiresAtUtc,
            legacyShare.IsPublic,
            false,
            legacySubjectType,
            legacySubjectId);
    }

    private static string? NormalizeExtension(string? extension, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(extension))
        {
            var trimmed = extension.Trim();
            return trimmed.StartsWith('.') ? trimmed[1..] : trimmed;
        }

        var lastDot = fileName.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < fileName.Length - 1)
        {
            return fileName[(lastDot + 1)..];
        }

        return null;
    }

    private static Uri CreateUri(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        return new Uri(value, UriKind.Relative);
    }

    private static string NormalizeShareSubjectType(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            var raw = value.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "public";
            }

            return raw.Trim().ToLowerInvariant() switch
            {
                "user" => "user",
                "group" => "group",
                _ => "public",
            };
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
        {
            return intValue switch
            {
                0 => "user",
                1 => "group",
                _ => "public",
            };
        }

        return "public";
    }

    public async Task<IReadOnlyCollection<TagLabelDto>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, "api/ecm/tags", cancellationToken);
        var response = await SendAsync<IReadOnlyCollection<TagLabelDto>>(request, cancellationToken);
        return response ?? [];
    }

    public async Task<TagLabelDto?> CreateTagAsync(CreateTagRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, "api/ecm/tags", cancellationToken);
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<TagLabelDto>(request, cancellationToken);
    }

    public async Task<TagLabelDto?> UpdateTagAsync(Guid tagId, UpdateTagRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Put, $"api/ecm/tags/{tagId}", cancellationToken);
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync<TagLabelDto>(request, cancellationToken);
    }

    public async Task<bool> DeleteTagAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Delete, $"api/ecm/tags/{tagId}", cancellationToken);
        return await SendAsync(request, cancellationToken);
    }

    public async Task<bool> AssignTagToDocumentAsync(Guid documentId, AssignTagRequestDto requestDto, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, $"api/ecm/documents/{documentId}/tags", cancellationToken);
        request.Content = JsonContent.Create(requestDto);
        return await SendAsync(request, cancellationToken);
    }

    public async Task<bool> RemoveTagFromDocumentAsync(Guid documentId, Guid tagId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Delete, $"api/ecm/documents/{documentId}/tags/{tagId}", cancellationToken);
        return await SendAsync(request, cancellationToken);
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

    private static string BuildDocumentListUri(ListDocumentsRequestDto request)
    {
        var query = new Dictionary<string, string?>();

        if (request.Page > 0)
        {
            query["page"] = request.Page.ToString();
        }

        if (request.PageSize > 0)
        {
            query["pageSize"] = request.PageSize.ToString();
        }

        if (!string.IsNullOrWhiteSpace(request.DocType))
        {
            query["doc_type"] = request.DocType;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query["status"] = request.Status;
        }

        if (!string.IsNullOrWhiteSpace(request.Sensitivity))
        {
            query["sensitivity"] = request.Sensitivity;
        }

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            query["q"] = request.Query;
        }

        if (request.OwnerId.HasValue)
        {
            query["owner_id"] = request.OwnerId.Value.ToString();
        }

        if (request.GroupId.HasValue)
        {
            query["group_id"] = request.GroupId.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(request.Sort))
        {
            query["sort"] = request.Sort;
        }

        var uri = QueryHelpers.AddQueryString("api/ecm/documents", query);

        if (request.GroupIds is { Length: > 0 })
        {
            var normalizedGroupIds = request.GroupIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();

            foreach (var groupId in normalizedGroupIds)
            {
                uri = QueryHelpers.AddQueryString(uri, "group_ids", groupId.ToString());
            }
        }

        if (request.Tags is { Length: > 0 })
        {
            foreach (var tag in request.Tags)
            {
                uri = QueryHelpers.AddQueryString(uri, "tags[]", tag.ToString());
            }
        }

        return uri;
    }

    private async Task<UserSummaryDto?> ResolvePasswordLoginProfileAsync(
        ClaimsPrincipal? principal,
        CancellationToken cancellationToken)
    {
        if (principal is null)
        {
            return null;
        }

        var cachedProfile = PasswordLoginClaims.GetProfileFromPrincipal(principal, out _);
        var userId = ResolveUserIdentifier(principal) ?? cachedProfile?.Id;

        if (userId is not null)
        {
            try
            {
                var profile = await GetUserAsync(userId.Value, cancellationToken);
                if (profile is not null)
                {
                    return profile;
                }
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to resolve profile for password login principal {UserId}; falling back to cached claims.",
                    userId);
            }
        }

        return cachedProfile;
    }

    private static Guid? ResolveUserIdentifier(ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return null;
        }

        var identifier = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(identifier, out var userId) ? userId : null;
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        HttpMethod method,
        string uri,
        CancellationToken cancellationToken,
        bool includeAuthentication = true,
        bool allowAppTokenFallback = true)
    {
        var request = new HttpRequestMessage(method, uri);
        AttachApiKeyHeader(request);
        if (includeAuthentication)
        {
            await AttachAuthenticationAsync(request, allowAppTokenFallback, cancellationToken);
        }

        ApplyForwardedHeaders(request);
        AttachPasswordLoginHeaders(request);

        return request;
    }

    private void AttachApiKeyHeader(HttpRequestMessage request)
    {
        var apiKey = _httpContextAccessor.HttpContext?.Request.Headers[ApiKeyHeaderName]
            ?? StringValues.Empty;

        if (StringValues.IsNullOrEmpty(apiKey))
        {
            return;
        }

        request.Headers.TryAddWithoutValidation(ApiKeyHeaderName, apiKey.AsEnumerable());
    }

    private void AttachPasswordLoginHeaders(HttpRequestMessage request)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (!PasswordLoginClaims.IsPasswordLoginPrincipal(principal))
        {
            return;
        }

        var profile = PasswordLoginClaims.GetProfileFromPrincipal(principal, out var invalidProfileClaim);
        if (profile is null)
        {
            if (invalidProfileClaim)
            {
                _logger.LogWarning("Password login profile claim was invalid. Skipping forwarded headers.");
            }

            return;
        }

        request.Headers.TryAddWithoutValidation(PasswordLoginForwardingHeaders.UserId, profile.Id.ToString());
        request.Headers.TryAddWithoutValidation(PasswordLoginForwardingHeaders.Email, profile.Email);
        request.Headers.TryAddWithoutValidation(PasswordLoginForwardingHeaders.PreferredUsername, profile.Email);

        if (!string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            request.Headers.TryAddWithoutValidation(PasswordLoginForwardingHeaders.DisplayName, profile.DisplayName);
        }

        if (profile.PrimaryGroupId is { } primaryGroupId && primaryGroupId != Guid.Empty)
        {
            request.Headers.TryAddWithoutValidation(PasswordLoginForwardingHeaders.PrimaryGroupId, primaryGroupId.ToString());

            var primaryGroupName = profile.Groups?
                .FirstOrDefault(group => group.Id == primaryGroupId)?
                .Name;

            if (!string.IsNullOrWhiteSpace(primaryGroupName))
            {
                request.Headers.TryAddWithoutValidation(
                    PasswordLoginForwardingHeaders.PrimaryGroupName,
                    primaryGroupName);
            }
        }

        if (principal?.HasClaim(PasswordLoginClaims.OnBehalfClaimType, "true") == true)
        {
            request.Headers.TryAddWithoutValidation(PasswordLoginForwardingHeaders.OnBehalf, "true");
        }
    }

    private void ApplyForwardedHeaders(HttpRequestMessage request)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        var origin = httpContext.Request;

        if (!string.IsNullOrWhiteSpace(origin.Scheme))
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", origin.Scheme);
        }

        if (origin.Host.HasValue)
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-Host", origin.Host.Value);

            if (origin.Host.Port is > 0)
            {
                request.Headers.TryAddWithoutValidation(
                    "X-Forwarded-Port",
                    origin.Host.Port.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        if (origin.PathBase.HasValue && origin.PathBase != PathString.Empty)
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-Prefix", origin.PathBase.Value);
        }
    }

    private async Task AttachAuthenticationAsync(
    HttpRequestMessage request,
    bool allowAppTokenFallback,
    CancellationToken cancellationToken)
    {
        // 1. Try attach Authorization from current HttpContext
        if (TryGetAuthorizationFromContext(request))
            return;

        // 2. Validate scopes
        var scopes = ValidateScopes(allowAppTokenFallback);
        if (scopes is null)
            return;

        var scheme = string.IsNullOrWhiteSpace(_options.AuthenticationScheme)
            ? OpenIdConnectDefaults.AuthenticationScheme
            : _options.AuthenticationScheme;

        var principal = EnsureHomeAccountIdentifiers(_httpContextAccessor.HttpContext?.User);
        var identity = principal?.Identities.FirstOrDefault(i =>
            i.IsAuthenticated &&
            string.Equals(i.AuthenticationType, scheme, StringComparison.OrdinalIgnoreCase));

        ClaimsPrincipal? tokenPrincipal = null;

        if (identity is not null)
        {
            tokenPrincipal = new ClaimsPrincipal(identity);
        }
        else if (principal?.Identity?.IsAuthenticated == true)
        {
            tokenPrincipal = principal;
        }


        // 3. Try user token first
        var userToken = await TryAcquireUserTokenAsync(
            tokenPrincipal,
            scopes,
            scheme,
            allowAppTokenFallback,
            cancellationToken);

        if (!string.IsNullOrEmpty(userToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
            return;
        }

        if (tokenPrincipal?.Identity?.IsAuthenticated != true && !allowAppTokenFallback)
            throw new UnauthorizedAccessException("User authentication is required but no principal was available.");

        // 4. Fallback: App token
        var appScope = ScopeUtilities.TryGetAppScope(scopes);
        if (string.IsNullOrWhiteSpace(appScope))
        {
            _logger.LogWarning("Unable to determine an app scope from configured scopes: {Scopes}", string.Join(", ", scopes));
            return;
        }

        try
        {
            var appToken = await _tokenAcquisition.GetAccessTokenForAppAsync(
                appScope,
                authenticationScheme: scheme,
                tenant: _options.TenantId,
                tokenAcquisitionOptions: new TokenAcquisitionOptions { CancellationToken = cancellationToken });

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", appToken);
        }
        catch (MsalException ex)
        {
            _logger.LogError(ex, "Unable to acquire application token for scope {Scope}", _options.Scope);
            throw new HttpRequestException("Failed to acquire app token.", ex);
        }
    }

    private bool TryGetAuthorizationFromContext(HttpRequestMessage request)
    {
        var headerValues = _httpContextAccessor.HttpContext?.Request.Headers.Authorization;

        var header = headerValues.GetValueOrDefault();

        if (StringValues.IsNullOrEmpty(header))
            return false;

        if (!AuthenticationHeaderValue.TryParse(header.ToString(), out var parsed))
            return false;

        request.Headers.Authorization = parsed;
        return true;
    }



    private string[]? ValidateScopes(bool allowFallback)
    {
        if (string.IsNullOrWhiteSpace(_options.Scope))
        {
            if (!allowFallback)
                throw new UnauthorizedAccessException("No API scope configured.");
            return null;
        }

        var scopes = ScopeUtilities.ParseScopes(_options.Scope);
        if (scopes.Length == 0)
        {
            if (!allowFallback)
                throw new UnauthorizedAccessException("No valid scopes configured.");
            return null;
        }

        return scopes;
    }


    private async Task<string?> TryAcquireUserTokenAsync(
    ClaimsPrincipal? user,
    string[] scopes,
    string scheme,
    bool allowFallback,
    CancellationToken ct)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        try
        {
            return await _tokenAcquisition.GetAccessTokenForUserAsync(
                scopes,
                authenticationScheme: scheme,
                tenantId: _options.TenantId,
                user: user,
                tokenAcquisitionOptions: new TokenAcquisitionOptions { CancellationToken = ct });
        }
        catch (Exception ex) when (ex is MsalException or MicrosoftIdentityWebChallengeUserException)
        {
            _logger.LogWarning(ex, "User token acquisition failed. Fallback to app token.");
            if (!allowFallback)
                throw new UnauthorizedAccessException("Failed to acquire user access token.", ex);

            return null;
        }
    }



    private static ClaimsPrincipal? EnsureHomeAccountIdentifiers(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return principal;
        }

        if (string.IsNullOrWhiteSpace(principal.GetHomeObjectId()))
        {
            var objectId = principal.GetObjectId();
            if (!string.IsNullOrWhiteSpace(objectId) && !identity.HasClaim(c => c.Type == ClaimConstants.UniqueObjectIdentifier))
            {
                identity.AddClaim(new Claim(ClaimConstants.UniqueObjectIdentifier, objectId));
            }
        }

        if (string.IsNullOrWhiteSpace(principal.GetHomeTenantId()))
        {
            var tenantId = principal.GetTenantId();
            if (!string.IsNullOrWhiteSpace(tenantId) && !identity.HasClaim(c => c.Type == ClaimConstants.UniqueTenantIdentifier))
            {
                identity.AddClaim(new Claim(ClaimConstants.UniqueTenantIdentifier, tenantId));
            }
        }

        if (string.IsNullOrWhiteSpace(principal.FindFirstValue(HomeAccountIdClaimType)))
        {
            var objectId = principal.GetObjectId();
            var tenantId = principal.GetTenantId();

            if (!string.IsNullOrWhiteSpace(objectId)
                && !string.IsNullOrWhiteSpace(tenantId)
                && !identity.HasClaim(c => c.Type == HomeAccountIdClaimType))
            {
                identity.AddClaim(new Claim(HomeAccountIdClaimType, $"{objectId}.{tenantId}"));
            }
        }

        return principal;
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

    private static bool IsRedirectStatusCode(HttpStatusCode statusCode) => (int)statusCode is >= 300 and < 400;

    private static async Task<DocumentFileContent?> CreateDocumentFileContentAsync(
        HttpResponseMessage response,
        bool enableRangeProcessing,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (content.Length == 0 && response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var fileName = GetFileName(response.Content);
        var lastModified = response.Content.Headers.LastModified;
        var supportsRangeProcessing = enableRangeProcessing && response.Headers.AcceptRanges.Any(range => string.Equals(range, "bytes", StringComparison.OrdinalIgnoreCase));

        return new DocumentFileContent(content, contentType, fileName, lastModified, supportsRangeProcessing);
    }

    private static string? GetFileName(HttpContent content)
    {
        var disposition = content.Headers.ContentDisposition;
        if (disposition is null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(disposition.FileNameStar))
        {
            return disposition.FileNameStar;
        }

        if (string.IsNullOrEmpty(disposition.FileName))
        {
            return null;
        }

        return disposition.FileName.Trim('\"');
    }

}
