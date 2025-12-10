using AppGateway.Api.Auth;
using AppGateway.Contracts.Signatures;
using AppGateway.Infrastructure.Ecm;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Api.Controllers;

[ApiController]
[Route("api/signatures")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class SignaturesController(ISignaturesApiClient client) : ControllerBase
{
    private readonly ISignaturesApiClient _client = client;

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
