using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Api.Auth;
using AppGateway.Contracts.Documents;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Api.Controllers.Documents;

[ApiController]
[Route("api/document-types")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class DocumentTypesController(IDocumentsApiClient client) : ControllerBase
{
    private readonly IDocumentsApiClient _client = client;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<DocumentTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<DocumentTypeDto>>> GetAsync(CancellationToken cancellationToken)
    {
        var documentTypes = await _client.GetDocumentTypesAsync(cancellationToken);
        return Ok(documentTypes);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DocumentTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostAsync([FromBody] DocumentTypeRequestDto request, CancellationToken cancellationToken)
    {
        var documentType = await _client.CreateDocumentTypeAsync(request, cancellationToken);
        if (documentType is null)
        {
            return Problem(title: "Failed to create document type", statusCode: StatusCodes.Status400BadRequest);
        }

        return Created($"/api/document-types/{documentType.Id}", documentType);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DocumentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PutAsync(Guid id, [FromBody] DocumentTypeRequestDto request, CancellationToken cancellationToken)
    {
        var documentType = await _client.UpdateDocumentTypeAsync(id, request, cancellationToken);
        if (documentType is null)
        {
            return NotFound();
        }

        return Ok(documentType);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _client.DeleteDocumentTypeAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
