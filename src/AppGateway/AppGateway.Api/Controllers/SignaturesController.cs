using AppGateway.Contracts.Signatures;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Api.Controllers;

[ApiController]
[Route("api/gateway/signatures")]
[Authorize]
public sealed class SignaturesController(IEcmApiClient client) : ControllerBase
{
    private readonly IEcmApiClient _client = client;

    [HttpPost]
    [ProducesResponseType(typeof(SignatureReceiptDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestSignatureAsync([FromBody] SignatureRequestDto request, CancellationToken cancellationToken)
    {
        var receipt = await _client.CreateSignatureRequestAsync(request, cancellationToken);
        if (receipt is null)
        {
            return Problem(title: "Failed to create signature request", statusCode: StatusCodes.Status400BadRequest);
        }

        return CreatedAtAction(nameof(RequestSignatureAsync), new { id = receipt.RequestId }, receipt);
    }
}
