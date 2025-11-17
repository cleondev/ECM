using System.Threading;
using System.Threading.Tasks;
using AppGateway.Api.Auth;
using AppGateway.Contracts.IAM.Users;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Api.Controllers.IAM;

[ApiController]
[Route("api/iam/profile")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class IamUserProfileController(
    IEcmApiClient client) : ControllerBase
{
    private readonly IEcmApiClient _client = client;

    [HttpGet]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var profile = await _client.GetCurrentUserProfileAsync(cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PutAsync(
        [FromBody] UpdateUserProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var profile = await _client.UpdateCurrentUserProfileAsync(request, cancellationToken);
        if (profile is null)
        {
            var existing = await _client.GetCurrentUserProfileAsync(cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            return Problem(title: "Failed to update profile", statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(profile);
    }

    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePasswordAsync(
        [FromBody] UpdateUserPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _client.UpdateCurrentUserPasswordAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        if (result.IsNotFound)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(result.Content))
        {
            return StatusCode((int)result.StatusCode);
        }

        return new ContentResult
        {
            Content = result.Content,
            ContentType = string.IsNullOrWhiteSpace(result.ContentType)
                ? "application/json"
                : result.ContentType,
            StatusCode = (int)result.StatusCode
        };
    }
}
