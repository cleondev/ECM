using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using AppGateway.Api.Auth;
using AppGateway.Contracts.Documents;
using AppGateway.Contracts.Tags;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class DocumentsController(IEcmApiClient client) : ControllerBase
{
    private readonly IEcmApiClient _client = client;

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

        var upload = new CreateDocumentUpload
        {
            Title = request.Title,
            DocType = request.DocType,
            Status = request.Status,
            OwnerId = request.OwnerId,
            CreatedBy = request.CreatedBy,
            Department = request.Department,
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

        return CreatedAtAction(nameof(GetAsync), new { id = document.Id }, document);
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

        return File(
            file.Content,
            file.ContentType,
            fileDownloadName: file.FileName,
            lastModified: file.LastModifiedUtc,
            enableRangeProcessing: file.EnableRangeProcessing);
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

        return File(
            file.Content,
            file.ContentType,
            fileDownloadName: file.FileName,
            lastModified: file.LastModifiedUtc,
            enableRangeProcessing: file.EnableRangeProcessing);
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

    [StringLength(128)]
    public string? Department { get; init; }

    [StringLength(64)]
    public string? Sensitivity { get; init; }

    public Guid? DocumentTypeId { get; init; }

    [Required]
    public IFormFile File { get; init; } = null!;
}
