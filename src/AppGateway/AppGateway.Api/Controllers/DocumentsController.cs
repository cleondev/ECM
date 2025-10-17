using System.Collections.Generic;
using AppGateway.Contracts.Documents;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public sealed class DocumentsController(IEcmApiClient client) : ControllerBase
{
    private readonly IEcmApiClient _client = client;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<DocumentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<DocumentSummaryDto>> GetAsync(CancellationToken cancellationToken)
        => await _client.GetDocumentsAsync(cancellationToken);

    [HttpPost]
    [ProducesResponseType(typeof(DocumentSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostAsync([FromBody] CreateDocumentRequestDto request, CancellationToken cancellationToken)
    {
        var document = await _client.CreateDocumentAsync(request, cancellationToken);
        if (document is null)
        {
            return Problem(title: "Failed to create document", statusCode: StatusCodes.Status400BadRequest);
        }

        return CreatedAtAction(nameof(GetAsync), new { id = document.Id }, document);
    }
}
