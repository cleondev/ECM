using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AppGateway.Api.Auth;
using AppGateway.Contracts.Documents;
using AppGateway.Contracts.Tags;
using AppGateway.Contracts.Workflows;
using AppGateway.Infrastructure.Ecm;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

using Shared.Extensions.Http;
using Shared.Extensions.Primitives;

using static AppGateway.Api.Controllers.Documents.DocumentRequestNormalization;

namespace AppGateway.Api.Controllers.Documents;

[ApiController]
[Route("api/documents")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class DocumentsController(IEcmApiClient client, ILogger<DocumentsController> logger) : ControllerBase
{
    private readonly IEcmApiClient _client = client;
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

        var documents = await _client.GetDocumentsAsync(request, cancellationToken);
        return Ok(documents);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostAsync([FromForm] CreateDocumentForm request, CancellationToken cancellationToken)
    {
        if (request.OwnerId is null || request.OwnerId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(request.OwnerId), "A valid owner is required.");
        }

        if (request.CreatedBy is null || request.CreatedBy == Guid.Empty)
        {
            ModelState.AddModelError(nameof(request.CreatedBy), "A valid creator is required.");
        }

        if (request.File is null || request.File.Length <= 0)
        {
            ModelState.AddModelError("file", "A non-empty file is required.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var (normalizedGroupId, normalizedGroupIds) = await DocumentUserContextResolver.ResolveGroupSelectionAsync(
            _client,
            _logger,
            request.CreatedBy.Value,
            cancellationToken);

        var upload = new CreateDocumentUpload
        {
            Title = request.Title,
            DocType = request.DocType,
            Status = request.Status,
            OwnerId = request.OwnerId.Value,
            CreatedBy = request.CreatedBy.Value,
            GroupId = normalizedGroupId,
            GroupIds = normalizedGroupIds,
            Sensitivity = request.Sensitivity,
            DocumentTypeId = request.DocumentTypeId,
            FileName = string.IsNullOrWhiteSpace(request.File.FileName) ? "upload.bin" : request.File.FileName,
            ContentType = string.IsNullOrWhiteSpace(request.File.ContentType) ? "application/octet-stream" : request.File.ContentType,
            FileSize = request.File.Length,
            OpenReadStream = _ => Task.FromResult(request.File.OpenReadStream()),
        };

        var document = await _client.CreateDocumentAsync(upload, cancellationToken);
        if (document is null)
        {
            return Problem(title: "Failed to create document", statusCode: StatusCodes.Status400BadRequest);
        }

        return Created($"/api/documents/{document.Id}", document);
    }

    [HttpDelete("{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var deleted = await _client.DeleteDocumentAsync(documentId, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("batch")]
    [ProducesResponseType(typeof(DocumentBatchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostBatchAsync([FromForm] CreateDocumentsForm request, CancellationToken cancellationToken)
    {
        if (request.Files.Count == 0)
        {
            ModelState.AddModelError("files", "At least one file is required.");
            return ValidationProblem(ModelState);
        }

        var claimedUserId = await DocumentUserContextResolver.ResolveUserIdAsync(
            _client,
            _logger,
            User,
            cancellationToken);
        var createdBy = NormalizeGuid(request.CreatedBy) ?? claimedUserId;
        var ownerId = NormalizeGuid(request.OwnerId) ?? createdBy;

        if (createdBy is null)
        {
            ModelState.AddModelError(nameof(request.CreatedBy), "The creator could not be determined from the request or user context.");
            return ValidationProblem(ModelState);
        }

        if (ownerId is null)
        {
            ModelState.AddModelError(nameof(request.OwnerId), "The owner could not be determined from the request or user context.");
            return ValidationProblem(ModelState);
        }

        var normalizedDocType = NormalizeString(request.DocType, "General");
        var normalizedStatus = NormalizeString(request.Status, "Draft");
        var normalizedSensitivity = NormalizeString(request.Sensitivity, "Internal");
        var (normalizedGroupId, normalizedGroupIds) = await DocumentUserContextResolver.ResolveGroupSelectionAsync(
            _client,
            _logger,
            createdBy.Value,
            cancellationToken);
        var documentTypeId = request.DocumentTypeId;
        var flowDefinition = NormalizeOptional(request.FlowDefinition);
        var tagIds = request.TagIds.Count == 0
            ? []
            : request.TagIds.Where(id => id != Guid.Empty).Distinct().ToArray();

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
                failures.Add(new DocumentUploadFailureDto(file.FileName ?? "upload.bin", "The uploaded file was empty."));
                continue;
            }

            var requestedTitle = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
            if (requestedTitle is not null && request.Files.Count > 1)
            {
                requestedTitle = $"{requestedTitle} ({index + 1})";
            }

            var upload = new CreateDocumentUpload
            {
                Title = NormalizeTitle(requestedTitle, file.FileName),
                DocType = normalizedDocType,
                Status = normalizedStatus,
                OwnerId = ownerId.Value,
                CreatedBy = createdBy.Value,
                GroupId = normalizedGroupId,
                GroupIds = normalizedGroupIds,
                Sensitivity = normalizedSensitivity,
                DocumentTypeId = documentTypeId,
                FileName = string.IsNullOrWhiteSpace(file.FileName) ? "upload.bin" : file.FileName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                FileSize = file.Length,
                OpenReadStream = _ => Task.FromResult(file.OpenReadStream()),
            };

            try
            {
                var document = await _client.CreateDocumentAsync(upload, cancellationToken);
                if (document is null)
                {
                    failures.Add(new DocumentUploadFailureDto(upload.FileName, "The document service returned an empty response."));
                    continue;
                }

                documents.Add(document);

                await DocumentPostUploadActions.AssignTagsAsync(
                    _client,
                    _logger,
                    document.Id,
                    tagIds,
                    createdBy.Value,
                    cancellationToken);

                await DocumentPostUploadActions.StartWorkflowAsync(
                    _client,
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
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var downloadUri = await _client.GetDocumentVersionDownloadUriAsync(versionId, cancellationToken);
        if (downloadUri is null)
        {
            return NotFound();
        }

        return Redirect(downloadUri.ToString());
    }

    [HttpGet("files/preview/{versionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var file = await _client.GetDocumentVersionPreviewAsync(versionId, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        return DocumentFileResultFactory.Create(file);
    }

    [HttpPost("files/share/{versionId:guid}")]
    [ProducesResponseType(typeof(DocumentShareLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ShareVersionAsync(
        Guid versionId,
        [FromBody] CreateShareLinkRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.DocumentId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(request.DocumentId), "Document identifier is required.");
        }

        var effectiveVersionId = request.VersionId == Guid.Empty ? versionId : request.VersionId;
        if (effectiveVersionId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(request.VersionId), "Version identifier is required.");
        }
        else if (effectiveVersionId != versionId)
        {
            ModelState.AddModelError(nameof(request.VersionId), "Version identifier does not match the route parameter.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var normalizedRequest = request with { VersionId = versionId };
        var link = await _client.CreateDocumentShareLinkAsync(normalizedRequest, cancellationToken);
        if (link is null)
        {
            return NotFound();
        }

        return Ok(link);
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

        var normalizedFit = string.IsNullOrWhiteSpace(fit)
            ? null
            : fit.Trim().ToLowerInvariant();

        if (normalizedFit is not null && normalizedFit != "cover" && normalizedFit != "contain")
        {
            ModelState.AddModelError("fit", "Parameter 'fit' must be either 'cover' or 'contain'.");
            return ValidationProblem(ModelState);
        }

        var file = await _client.GetDocumentVersionThumbnailAsync(versionId, width, height, normalizedFit, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        return DocumentFileResultFactory.Create(file);
    }

    [HttpPost("{documentId:guid}/tags")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignTagAsync(Guid documentId, [FromBody] AssignTagRequestDto request, CancellationToken cancellationToken)
    {
        if (request.TagId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(request.TagId), "Tag identifier is required.");
            return ValidationProblem(ModelState);
        }

        var assigned = await _client.AssignTagToDocumentAsync(documentId, request, cancellationToken);
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
        if (tagId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(tagId), "Tag identifier is required.");
            return ValidationProblem(ModelState);
        }

        var removed = await _client.RemoveTagFromDocumentAsync(documentId, tagId, cancellationToken);
        if (!removed)
        {
            return Problem(title: "Failed to remove tag from document", statusCode: StatusCodes.Status400BadRequest);
        }

        return NoContent();
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

        return parsed.HasValues ? [.. parsed.Values] : [];
    }

    private static string? ReadQueryString(IQueryCollection query, IEnumerable<string> keys)
    {
        if (!query.TryGetString(keys, out var value))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string FormatInvalidValue(string? raw)
    {
        return string.IsNullOrWhiteSpace(raw)
            ? "The provided value is not valid."
            : $"The value '{raw}' is not valid.";
    }
}
