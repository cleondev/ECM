using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using AppGateway.Contracts.Documents;
using AppGateway.Api.Auth;
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
