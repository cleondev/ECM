using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
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

namespace AppGateway.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class DocumentsController(IEcmApiClient client, ILogger<DocumentsController> logger) : ControllerBase
{
    private readonly IEcmApiClient _client = client;
    private readonly ILogger<DocumentsController> _logger = logger;

    [HttpGet]
    [ProducesResponseType(typeof(DocumentListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentListDto>> GetAsync([FromQuery] ListDocumentsRequestDto request, CancellationToken cancellationToken)
    {
        var documents = await _client.GetDocumentsAsync(request ?? ListDocumentsRequestDto.Default, cancellationToken);
        return Ok(documents);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostAsync([FromForm] CreateDocumentForm request, CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length <= 0)
        {
            ModelState.AddModelError("file", "A non-empty file is required.");
            return ValidationProblem(ModelState);
        }

        var formCollection = Request.HasFormContentType ? Request.Form : null;
        var parsedGroupIds = formCollection is null
            ? []
            : ParseGroupIds(formCollection);
        var normalizedGroupIds = NormalizeGroupSelection(NormalizeGuid(request.GroupId), parsedGroupIds, out var normalizedGroupId);

        var upload = new CreateDocumentUpload
        {
            Title = request.Title,
            DocType = request.DocType,
            Status = request.Status,
            OwnerId = request.OwnerId,
            CreatedBy = request.CreatedBy,
            GroupId = normalizedGroupId,
            GroupIds = normalizedGroupIds,
            Sensitivity = request.Sensitivity,
            DocumentTypeId = request.DocumentTypeId,
            FileName = string.IsNullOrWhiteSpace(request.File.FileName) ? "upload.bin" : request.File.FileName,
            ContentType = string.IsNullOrWhiteSpace(request.File.ContentType) ? "application/octet-stream" : request.File.ContentType,
            FileSize = request.File.Length,
            OpenReadStream = _ => Task.FromResult<Stream>(request.File.OpenReadStream()),
        };

        var document = await _client.CreateDocumentAsync(upload, cancellationToken);
        if (document is null)
        {
            return Problem(title: "Failed to create document", statusCode: StatusCodes.Status400BadRequest);
        }

        return Created($"/api/documents/{document.Id}", document);
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

        var claimedUserId = await ResolveUserIdAsync(User, cancellationToken);
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
        var normalizedGroupId = NormalizeGuid(request.GroupId);
        var normalizedGroupIds = NormalizeGroupSelection(normalizedGroupId, request.GroupIds, out var resolvedGroupId);
        normalizedGroupId = resolvedGroupId;
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
                OpenReadStream = _ => Task.FromResult<Stream>(file.OpenReadStream()),
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

                await AssignTagsAsync(document.Id, tagIds, createdBy.Value, cancellationToken);
                await StartWorkflowAsync(document.Id, flowDefinition, cancellationToken);
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

        return CreateFileResult(file);
    }

    [HttpPost("files/share/{versionId:guid}")]
    [ProducesResponseType(typeof(DocumentShareLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ShareVersionAsync(
        Guid versionId,
        [FromBody] CreateShareLinkRequestDto request,
        CancellationToken cancellationToken)
    {
        var link = await _client.CreateDocumentShareLinkAsync(versionId, request, cancellationToken);
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

        return CreateFileResult(file);
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

    private static FileContentResult CreateFileResult(DocumentFileContent file)
    {
        return new FileContentResult(file.Content, file.ContentType)
        {
            FileDownloadName = file.FileName,
            LastModified = file.LastModifiedUtc,
            EnableRangeProcessing = file.EnableRangeProcessing,
        };
    }

    private async Task AssignTagsAsync(Guid documentId, IReadOnlyCollection<Guid> tagIds, Guid appliedBy, CancellationToken cancellationToken)
    {
        if (tagIds.Count == 0)
        {
            return;
        }

        foreach (var tagId in tagIds)
        {
            try
            {
                var assigned = await _client.AssignTagToDocumentAsync(documentId, new AssignTagRequestDto(tagId, appliedBy), cancellationToken);
                if (!assigned)
                {
                    _logger.LogWarning("Failed to assign tag {TagId} to document {DocumentId}", tagId, documentId);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to assign tag {TagId} to document {DocumentId}", tagId, documentId);
            }
        }
    }

    private async Task StartWorkflowAsync(Guid documentId, string? flowDefinition, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(flowDefinition))
        {
            return;
        }

        try
        {
            await _client.StartWorkflowAsync(new StartWorkflowRequestDto
            {
                DocumentId = documentId,
                Definition = flowDefinition,
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to start workflow {Workflow} for document {DocumentId}", flowDefinition, documentId);
        }
    }

    private static Guid? NormalizeGuid(Guid? value)
    {
        return value is null || value == Guid.Empty ? null : value;
    }

    private static string NormalizeString(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeTitle(string? title, string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            if (!string.IsNullOrWhiteSpace(nameWithoutExtension))
            {
                return nameWithoutExtension.Trim();
            }

            return fileName.Trim();
        }

        return "Untitled document";
    }

    private static IReadOnlyList<Guid> ParseGroupIds(IFormCollection? form)
    {
        if (form is null)
        {
            return [];
        }

        return ParseGuidList(form, "GroupIds", "groupIds", "group_ids", "GroupIds[]", "groupIds[]", "group_ids[]");
    }

    private static IReadOnlyList<Guid> ParseGuidList(IFormCollection form, params string[] fieldNames)
    {
        foreach (var field in fieldNames)
        {
            if (!form.TryGetValue(field, out var values) || values.Count == 0)
            {
                continue;
            }

            var parsed = ParseGuidValues(values);
            if (parsed.Count > 0)
            {
                return parsed;
            }
        }

        return [];
    }

    private static IReadOnlyList<Guid> ParseGuidValues(StringValues values)
    {
        var buffer = new List<Guid>();
        var seen = new HashSet<Guid>();

        void Add(Guid value)
        {
            if (value == Guid.Empty)
            {
                return;
            }

            if (seen.Add(value))
            {
                buffer.Add(value);
            }
        }

        if (values.Count > 1)
        {
            foreach (var value in values)
            {
                if (Guid.TryParse(value, out var guid))
                {
                    Add(guid);
                }
            }

            return buffer;
        }

        var raw = values[0];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return buffer;
        }

        if (raw.TrimStart().StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<string[]>(raw);
                if (parsed is not null)
                {
                    foreach (var candidate in parsed)
                    {
                        if (Guid.TryParse(candidate, out var guid))
                        {
                            Add(guid);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // fall through to delimiter parsing
            }
        }

        if (buffer.Count > 0)
        {
            return buffer;
        }

        foreach (var segment in raw.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Guid.TryParse(segment, out var guid))
            {
                Add(guid);
            }
        }

        return buffer;
    }

    private static IReadOnlyList<Guid> NormalizeGroupSelection(Guid? groupId, IReadOnlyCollection<Guid> groupIds, out Guid? primaryGroupId)
    {
        var buffer = new List<Guid>();
        var seen = new HashSet<Guid>();

        if (groupId.HasValue && groupId.Value != Guid.Empty && seen.Add(groupId.Value))
        {
            buffer.Add(groupId.Value);
        }

        if (groupIds is not null)
        {
            foreach (var id in groupIds)
            {
                if (id == Guid.Empty)
                {
                    continue;
                }

                if (seen.Add(id))
                {
                    buffer.Add(id);
                }
            }
        }

        primaryGroupId = buffer.Count > 0 ? buffer[0] : (Guid?)null;
        return buffer;
    }

    private async Task<Guid?> ResolveUserIdAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken)
    {
        if (principal is null)
        {
            return null;
        }

        var upn = ResolveUserPrincipalName(principal);
        if (string.IsNullOrWhiteSpace(upn))
        {
            return null;
        }

        try
        {
            var user = await _client.GetUserByEmailAsync(upn, cancellationToken);
            return user?.Id;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to resolve user id from UPN {Upn}", upn);
            return null;
        }
    }

    private static string? ResolveUserPrincipalName(ClaimsPrincipal principal)
    {
        foreach (var claimType in new[] { ClaimTypes.Upn, "preferred_username", ClaimTypes.Email })
        {
            var value = principal.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}

public sealed class CreateDocumentForm
{
    [Required]
    [StringLength(256)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string DocType { get; init; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string Status { get; init; } = string.Empty;

    [Required]
    public Guid OwnerId { get; init; }

    [Required]
    public Guid CreatedBy { get; init; }

    public Guid? GroupId { get; init; }

    public IReadOnlyCollection<Guid> GroupIds { get; init; } = [];

    [StringLength(64)]
    public string? Sensitivity { get; init; }

    public Guid? DocumentTypeId { get; init; }

    [Required]
    public IFormFile File { get; init; } = null!;
}

public sealed class CreateDocumentsForm
{
    public string? Title { get; init; }

    public string? DocType { get; init; }

    public string? Status { get; init; }

    public Guid? OwnerId { get; init; }

    public Guid? CreatedBy { get; init; }

    public Guid? GroupId { get; init; }

    public IReadOnlyList<Guid> GroupIds { get; init; } = [];

    public string? Sensitivity { get; init; }

    public Guid? DocumentTypeId { get; init; }

    public string? FlowDefinition { get; init; }

    public IReadOnlyList<Guid> TagIds { get; init; } = [];

    public IReadOnlyList<IFormFile> Files { get; init; } = [];

    public static async ValueTask<CreateDocumentsForm?> BindAsync(HttpContext context)
    {
        if (!context.Request.HasFormContentType)
        {
            return null;
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);

        var request = new CreateDocumentsForm
        {
            Title = GetString(form, nameof(Title)),
            DocType = GetString(form, nameof(DocType)),
            Status = GetString(form, nameof(Status)),
            OwnerId = GetGuid(form, nameof(OwnerId)),
            CreatedBy = GetGuid(form, nameof(CreatedBy)),
            GroupId = GetGuid(form, nameof(GroupId)),
            GroupIds = GetGuidList(form, nameof(GroupIds)),
            Sensitivity = GetString(form, nameof(Sensitivity)),
            DocumentTypeId = GetGuid(form, nameof(DocumentTypeId)),
            FlowDefinition = GetString(form, nameof(FlowDefinition)),
            TagIds = GetGuidList(form, "Tags"),
            Files = GetFiles(form),
        };

        return request;
    }

    private static string? GetString(IFormCollection form, string propertyName)
    {
        if (form.TryGetValue(propertyName, out var value) && !StringValues.IsNullOrEmpty(value))
        {
            return value.ToString();
        }

        var camelCase = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
        if (form.TryGetValue(camelCase, out value) && !StringValues.IsNullOrEmpty(value))
        {
            return value.ToString();
        }

        return null;
    }

    private static Guid? GetGuid(IFormCollection form, string propertyName)
    {
        var value = GetString(form, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    private static IReadOnlyList<Guid> GetGuidList(IFormCollection form, string propertyName)
    {
        foreach (var field in EnumerateFieldNames(propertyName))
        {
            if (!form.TryGetValue(field, out var values) || values.Count == 0)
            {
                continue;
            }

            var parsed = ParseGuidValues(values);
            if (parsed.Count > 0)
            {
                return parsed;
            }
        }

        return [];
    }

    private static IReadOnlyList<IFormFile> GetFiles(IFormCollection form)
    {
        if (form.Files.Count == 0)
        {
            return [];
        }

        var files = form.Files.GetFiles("Files");
        if (files.Count > 0)
        {
            return files.ToList();
        }

        files = form.Files.GetFiles("files");
        if (files.Count > 0)
        {
            return files.ToList();
        }

        return form.Files.ToList();
    }

    private static IEnumerable<string> EnumerateFieldNames(string propertyName)
    {
        yield return propertyName;
        yield return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];

        if (propertyName.Equals("GroupIds", StringComparison.Ordinal))
        {
            yield return "group_ids";
            yield return "GroupIds[]";
            yield return "groupIds[]";
            yield return "group_ids[]";
        }

        if (propertyName.Equals("Tags", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Tags[]";
            yield return "tags[]";
        }
    }

    private static IReadOnlyList<Guid> ParseGuidValues(StringValues values)
    {
        var buffer = new List<Guid>();
        var seen = new HashSet<Guid>();

        void Add(Guid value)
        {
            if (value == Guid.Empty)
            {
                return;
            }

            if (seen.Add(value))
            {
                buffer.Add(value);
            }
        }

        if (values.Count > 1)
        {
            foreach (var value in values)
            {
                if (Guid.TryParse(value, out var guid))
                {
                    Add(guid);
                }
            }

            return buffer;
        }

        var raw = values[0];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return buffer;
        }

        if (raw.TrimStart().StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<string[]>(raw);
                if (parsed is not null)
                {
                    foreach (var candidate in parsed)
                    {
                        if (Guid.TryParse(candidate, out var guid))
                        {
                            Add(guid);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // fall through to delimiter parsing
            }
        }

        if (buffer.Count > 0)
        {
            return buffer;
        }

        foreach (var segment in raw.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Guid.TryParse(segment, out var guid))
            {
                Add(guid);
            }
        }

        return buffer;
    }
}
