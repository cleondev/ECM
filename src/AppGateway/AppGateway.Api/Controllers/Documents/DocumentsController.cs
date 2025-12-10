using System.Security.Claims;

using AppGateway.Api.Auth;
using AppGateway.Api.Fake;
using AppGateway.Contracts.Documents;
using AppGateway.Contracts.Tags;
using AppGateway.Infrastructure.Ecm;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Shared.Extensions.Http;
using Shared.Extensions.Primitives;

namespace AppGateway.Api.Controllers.Documents;

[ApiController]
[Route("api/documents")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class DocumentsController(
    IDocumentsApiClient documentsClient,
    IUsersApiClient usersClient,
    ITagsApiClient tagsClient,
    IWorkflowsApiClient workflowsClient,
    ILogger<DocumentsController> logger) : ControllerBase
{
    private readonly IDocumentsApiClient _documentsClient = documentsClient;
    private readonly IUsersApiClient _usersClient = usersClient;
    private readonly ITagsApiClient _tagsClient = tagsClient;
    private readonly IWorkflowsApiClient _workflowsClient = workflowsClient;
    private readonly ILogger<DocumentsController> _logger = logger;

    [HttpGet]
    [ProducesResponseType(typeof(DocumentListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentListDto>> GetAsync(CancellationToken cancellationToken)
    {
        var request = BindListDocumentsRequest();

        if (!TryValidateModel(request))
        {
            return ValidationProblem(ModelState);
        }

        var documents = await _documentsClient.GetDocumentsAsync(request, cancellationToken);
        return Ok(documents);
    }

    [HttpGet("{documentId:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid documentId, CancellationToken cancellationToken)
    {
        if (!EnsureNotEmptyGuid(documentId, nameof(documentId), "A valid document identifier is required."))
        {
            return ValidationProblem(ModelState);
        }

        var document = await _documentsClient.GetDocumentAsync(documentId, cancellationToken);
        return document is null ? NotFound() : Ok(document);
    }

    [HttpGet("{documentId:guid}/flows")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyCollection<FakeFlowResponse>), StatusCodes.Status200OK)]
    public IActionResult GetFlows(Guid documentId)
    {
        if (!EnsureNotEmptyGuid(documentId, nameof(documentId), "A valid document identifier is required."))
        {
            return ValidationProblem(ModelState);
        }

        var flows = FakeEcmData.GetFlows(documentId);
        return Ok(flows);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostAsync([FromForm] CreateDocumentForm request, CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length <= 0)
        {
            ModelState.AddModelError("file", "A non-empty file is required.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var primaryGroupId = await ResolvePrimaryGroupIdAsync(cancellationToken);

        var upload = new CreateDocumentUpload
        {
            Title = request.Title,
            DocType = request.DocType,
            Status = request.Status,
            OwnerId = request.OwnerId,
            CreatedBy = request.CreatedBy,
            GroupId = primaryGroupId,
            Sensitivity = request.Sensitivity,
            DocumentTypeId = request.DocumentTypeId,
            FileName = NormalizeFileName(request.File.FileName, "upload.bin"),
            ContentType = NormalizeContentType(request.File.ContentType),
            FileSize = request.File.Length,
            OpenReadStream = _ => Task.FromResult(request.File.OpenReadStream()),
        };

        var document = await _documentsClient.CreateDocumentAsync(upload, cancellationToken);
        if (document is null)
        {
            return Problem(title: "Failed to create document", statusCode: StatusCodes.Status400BadRequest);
        }

        return Created($"/api/documents/{document.Id}", document);
    }

    [HttpPut("{documentId:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutAsync(
        Guid documentId,
        [FromBody] UpdateDocumentRequestDto? request,
        CancellationToken cancellationToken)
    {
        if (!EnsureNotEmptyGuid(documentId, nameof(documentId), "A valid document identifier is required."))
        {
            return ValidationProblem(ModelState);
        }

        request ??= new UpdateDocumentRequestDto();

        if (request.HasGroupId && !EnsureNotEmptyGuid(request.GroupId, nameof(request.GroupId), "Group identifier must be a valid GUID when provided."))
        {
            return ValidationProblem(ModelState);
        }

        if (request.HasDocumentTypeId && !EnsureNotEmptyGuid(request.DocumentTypeId, nameof(request.DocumentTypeId), "Document type must be a valid GUID when provided."))
        {
            return ValidationProblem(ModelState);
        }

        var document = await _documentsClient.UpdateDocumentAsync(documentId, request, cancellationToken);
        return document is null ? NotFound() : Ok(document);
    }

    [HttpDelete("{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var deleted = await _documentsClient.DeleteDocumentAsync(documentId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpDelete("files/{versionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteByVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var deleted = await _documentsClient.DeleteDocumentByVersionAsync(versionId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("batch")]
    [ProducesResponseType(typeof(DocumentBatchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostBatchAsync(
        [ModelBinder(typeof(CreateDocumentsFormModelBinder))] CreateDocumentsForm request,
        CancellationToken cancellationToken)
    {
        if (request.Files.Count == 0)
        {
            ModelState.AddModelError("files", "At least one file is required.");
            return ValidationProblem(ModelState);
        }

        var createdBy = request.CreatedBy;
        var ownerId = request.OwnerId;
        var docType = request.DocType?.Trim() ?? string.Empty;
        var status = request.Status?.Trim() ?? string.Empty;
        var sensitivity = NormalizeOptional(request.Sensitivity);
        var documentTypeId = request.DocumentTypeId;
        var flowDefinition = NormalizeOptional(request.FlowDefinition);

        var primaryGroupId = await ResolvePrimaryGroupIdAsync(cancellationToken);
        var tagIds = request.TagIds.Count == 0
            ? Array.Empty<Guid>()
            : request.TagIds.Where(id => id != Guid.Empty).Distinct().ToArray();

        var tagActor = createdBy ?? ownerId;

        var documents = new List<DocumentDto>();
        var failures = new List<DocumentUploadFailureDto>();

        for (var index = 0; index < request.Files.Count; index++)
        {
            var file = request.Files[index];
            if (file is null)
            {
                continue;
            }

            if (file.Length <= 0)
            {
                failures.Add(new DocumentUploadFailureDto(
                    file.FileName ?? "upload.bin",
                    "The uploaded file was empty."));
                continue;
            }

            var requestedTitle = NormalizeOptional(request.Title);
            if (requestedTitle is not null && request.Files.Count > 1)
            {
                requestedTitle = $"{requestedTitle} ({index + 1})";
            }

            var upload = new CreateDocumentUpload
            {
                Title = requestedTitle ?? string.Empty,
                DocType = docType,
                Status = status,
                OwnerId = ownerId,
                CreatedBy = createdBy,
                GroupId = primaryGroupId,
                Sensitivity = sensitivity,
                DocumentTypeId = documentTypeId,
                FileName = NormalizeFileName(file.FileName, "upload.bin"),
                ContentType = NormalizeContentType(file.ContentType),
                FileSize = file.Length,
                OpenReadStream = _ => Task.FromResult(file.OpenReadStream()),
            };

            try
            {
                var document = await _documentsClient.CreateDocumentAsync(upload, cancellationToken);
                if (document is null)
                {
                    failures.Add(new DocumentUploadFailureDto(
                        upload.FileName,
                        "The document service returned an empty response."));
                    continue;
                }

                documents.Add(document);

                if (tagActor.HasValue)
                {
                    await DocumentPostUploadActions.AssignTagsAsync(
                        _tagsClient,
                        _logger,
                        document.Id,
                        tagIds,
                        tagActor.Value,
                        cancellationToken);
                }

                await DocumentPostUploadActions.StartWorkflowAsync(
                    _workflowsClient,
                    _logger,
                    document.Id,
                    flowDefinition,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to upload document {FileName}", upload.FileName);
                failures.Add(new DocumentUploadFailureDto(upload.FileName, exception.Message));
            }
        }

        if (documents.Count == 0)
        {
            ModelState.AddModelError("files", "None of the provided files could be uploaded successfully.");
            return ValidationProblem(ModelState);
        }

        var response = new DocumentBatchDto(documents, failures);
        return Created("/api/documents/batch", response);
    }

    [HttpGet("files/download/{versionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var file = await _documentsClient.DownloadDocumentVersionAsync(versionId, cancellationToken);
        return file is null ? NotFound() : DocumentFileResultFactory.Create(file);
    }

    [HttpGet("files/preview/{versionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PreviewVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var result = await _documentsClient.GetDocumentVersionPreviewAsync(versionId, cancellationToken);

        if (result.IsForbidden)
        {
            return Forbid(authenticationSchemes: [GatewayAuthenticationSchemes.Default]);
        }

        return result.Payload is null ? NotFound() : DocumentFileResultFactory.Create(result.Payload);
    }

    [HttpPost("files/share/{versionId:guid}")]
    [ProducesResponseType(typeof(DocumentShareLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ShareVersionAsync(
        Guid versionId,
        [FromBody] CreateShareLinkRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!EnsureNotEmptyGuid(request.DocumentId, nameof(request.DocumentId), "Document identifier is required."))
        {
            return ValidationProblem(ModelState);
        }

        var effectiveVersionId = request.VersionId == Guid.Empty ? versionId : request.VersionId;
        if (!EnsureNotEmptyGuid(effectiveVersionId, nameof(request.VersionId), "Version identifier is required."))
        {
            return ValidationProblem(ModelState);
        }

        if (effectiveVersionId != versionId)
        {
            ModelState.AddModelError(nameof(request.VersionId), "Version identifier does not match the route parameter.");
            return ValidationProblem(ModelState);
        }

        var normalizedSubjectType = request.GetNormalizedSubjectType();
        var normalizedSubjectId = request.GetEffectiveSubjectId();

        if (!string.IsNullOrWhiteSpace(request.SubjectType)
            && normalizedSubjectType == "public"
            && !string.Equals(request.SubjectType.Trim(), "public", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(request.SubjectType), "Subject type must be 'public', 'user', or 'group'.");
        }

        if (normalizedSubjectType is "user" or "group" && normalizedSubjectId is null)
        {
            ModelState.AddModelError(nameof(request.SubjectId), "A subject identifier is required when sharing with a user or group.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var normalizedRequest = request with
        {
            VersionId = versionId,
            SubjectType = normalizedSubjectType,
            SubjectId = normalizedSubjectId,
        };

        var link = await _documentsClient.CreateDocumentShareLinkAsync(normalizedRequest, cancellationToken);
        return link is null ? NotFound() : Ok(link);
    }

    [HttpGet("files/thumbnails/{versionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetThumbnailAsync(
        Guid versionId,
        [FromQuery(Name = "w")] int width,
        [FromQuery(Name = "h")] int height,
        [FromQuery(Name = "fit")] string? fit,
        CancellationToken cancellationToken)
    {
        if (width <= 0 || height <= 0)
        {
            ModelState.AddModelError("dimensions", "Parameters 'w' and 'h' must be positive integers.");
            return ValidationProblem(ModelState);
        }

        var normalizedFit = NormalizeOptional(fit)?.ToLowerInvariant();
        if (normalizedFit is not null && normalizedFit is not ("cover" or "contain"))
        {
            ModelState.AddModelError("fit", "Parameter 'fit' must be either 'cover' or 'contain'.");
            return ValidationProblem(ModelState);
        }

        var file = await _documentsClient.GetDocumentVersionThumbnailAsync(versionId, width, height, normalizedFit, cancellationToken);
        return file is null ? NotFound() : DocumentFileResultFactory.Create(file);
    }

    [HttpPost("{documentId:guid}/tags")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignTagAsync(Guid documentId, [FromBody] AssignTagRequestDto request, CancellationToken cancellationToken)
    {
        if (!EnsureNotEmptyGuid(request.TagId, nameof(request.TagId), "Tag identifier is required."))
        {
            return ValidationProblem(ModelState);
        }

        var assigned = await _tagsClient.AssignTagToDocumentAsync(documentId, request, cancellationToken);
        if (!assigned)
        {
            return Problem(title: "Failed to assign tag to document", statusCode: StatusCodes.Status400BadRequest);
        }

        return NoContent();
    }

    [HttpDelete("{documentId:guid}/tags/{tagId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveTagAsync(Guid documentId, Guid tagId, CancellationToken cancellationToken)
    {
        if (!EnsureNotEmptyGuid(tagId, nameof(tagId), "Tag identifier is required."))
        {
            return ValidationProblem(ModelState);
        }

        var removed = await _tagsClient.RemoveTagFromDocumentAsync(documentId, tagId, cancellationToken);
        if (!removed)
        {
            return Problem(title: "Failed to remove tag from document", statusCode: StatusCodes.Status400BadRequest);
        }

        return NoContent();
    }

    // ----------------- Helpers -----------------

    private async Task<Guid?> ResolvePrimaryGroupIdAsync(CancellationToken cancellationToken)
    {
        var claimValue = User.FindFirstValue("primary_group_id");
        if (Guid.TryParse(claimValue, out var parsed) && parsed != Guid.Empty)
        {
            return parsed;
        }

        var resolution = await CurrentUserProfileResolver.ResolveAsync(
            HttpContext,
            _usersClient,
            _logger,
            cancellationToken);

        var primaryGroupId = resolution.Profile?.PrimaryGroupId;
        return primaryGroupId is { } id && id != Guid.Empty ? id : null;
    }

    private ListDocumentsRequestDto BindListDocumentsRequest()
    {
        var defaults = ListDocumentsRequestDto.Default;
        var query = Request.Query;

        var page = defaults.Page;
        var pageResult = query.ParseValue<int>(["page", "Page"]);
        if (pageResult.TryGetValue(out var parsedPage))
        {
            page = parsedPage;
        }
        else if (pageResult.IsPresent)
        {
            ModelState.TryAddModelError(nameof(ListDocumentsRequestDto.Page), FormatInvalidValue(pageResult.RawValue));
        }

        var pageSize = defaults.PageSize;
        var pageSizeResult = query.ParseValue<int>(["page_size", "pageSize", "PageSize"]);
        if (pageSizeResult.TryGetValue(out var parsedPageSize))
        {
            pageSize = parsedPageSize;
        }
        else if (pageSizeResult.IsPresent)
        {
            ModelState.TryAddModelError(nameof(ListDocumentsRequestDto.PageSize), FormatInvalidValue(pageSizeResult.RawValue));
        }

        Guid? ownerId = null;
        var ownerResult = query.ParseValue<Guid>(["owner_id", "ownerId", "OwnerId"]);
        if (ownerResult.TryGetValue(out var parsedOwnerId))
        {
            ownerId = parsedOwnerId;
        }
        else if (ownerResult.IsPresent)
        {
            ModelState.TryAddModelError(nameof(ListDocumentsRequestDto.OwnerId), FormatInvalidValue(ownerResult.RawValue));
        }

        Guid? groupId = null;
        var groupResult = query.ParseValue<Guid>(["group_id", "groupId", "GroupId"]);
        if (groupResult.TryGetValue(out var parsedGroupId))
        {
            groupId = parsedGroupId;
        }
        else if (groupResult.IsPresent)
        {
            ModelState.TryAddModelError(nameof(ListDocumentsRequestDto.GroupId), FormatInvalidValue(groupResult.RawValue));
        }

        var groupIds = TryReadGuidArray(query, ["group_ids", "groupIds", "group_ids[]", "groupIds[]"], nameof(ListDocumentsRequestDto.GroupIds));
        var tags = TryReadGuidArray(query, ["tags[]", "tags"], nameof(ListDocumentsRequestDto.Tags));

        return new ListDocumentsRequestDto
        {
            Page = page,
            PageSize = pageSize,
            DocType = ReadQueryString(query, ["doc_type", "docType", nameof(ListDocumentsRequestDto.DocType)]),
            Status = ReadQueryString(query, ["status", nameof(ListDocumentsRequestDto.Status)]),
            Sensitivity = ReadQueryString(query, ["sensitivity", nameof(ListDocumentsRequestDto.Sensitivity)]),
            Query = ReadQueryString(query, ["q", "query", nameof(ListDocumentsRequestDto.Query)]),
            OwnerId = ownerId,
            GroupId = groupId,
            GroupIds = groupIds,
            Tags = tags,
            Sort = ReadQueryString(query, ["sort", nameof(ListDocumentsRequestDto.Sort)]),
        };
    }

    private Guid[]? TryReadGuidArray(IQueryCollection query, IEnumerable<string> keys, string propertyName)
    {
        if (!query.TryGetValues(keys, out var values))
        {
            return null;
        }

        var parsed = values.ParseGuidValues();
        if (parsed.HasInvalidValues)
        {
            foreach (var invalid in parsed.InvalidValues)
            {
                ModelState.TryAddModelError(propertyName, $"The value '{invalid}' is not valid.");
            }
        }

        return parsed.HasValues ? [.. parsed.Values] : Array.Empty<Guid>();
    }

    private static string? ReadQueryString(IQueryCollection query, IEnumerable<string> keys)
    {
        return query.TryGetString(keys, out var value)
            ? NormalizeOptional(value)
            : null;
    }

    private static string NormalizeContentType(string? contentType)
        => string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();

    private static string NormalizeFileName(string? fileName, string fallback)
        => string.IsNullOrWhiteSpace(fileName) ? fallback : fileName.Trim();

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatInvalidValue(string? raw)
        => string.IsNullOrWhiteSpace(raw)
            ? "The provided value is not valid."
            : $"The value '{raw}' is not valid.";

    private bool EnsureNotEmptyGuid(Guid? value, string key, string message)
    {
        if (value.HasValue && value.Value != Guid.Empty)
        {
            return true;
        }

        ModelState.AddModelError(key, message);
        return false;
    }
}
