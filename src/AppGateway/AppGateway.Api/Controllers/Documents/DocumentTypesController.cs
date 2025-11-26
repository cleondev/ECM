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
public sealed class DocumentTypesController(IEcmApiClient client) : ControllerBase
{
    private readonly IEcmApiClient _client = client;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<DocumentTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<DocumentTypeDto>>> GetAsync(CancellationToken cancellationToken)
    {
        var documentTypes = await _client.GetDocumentTypesAsync(cancellationToken);
        return Ok(documentTypes);
    }
}
