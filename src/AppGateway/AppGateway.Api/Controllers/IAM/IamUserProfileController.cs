using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Api.Auth;
using AppGateway.Contracts.IAM.Users;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Api.Controllers.IAM;

[ApiController]
[Route("api/iam/profile")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class IamUserProfileController(
    IUsersApiClient client) : ControllerBase
{
    private readonly IUsersApiClient _client = client;

    [HttpGet]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _client.GetCurrentUserProfileAsync(cancellationToken);
            return profile is null ? NotFound() : Ok(profile);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTokenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var authorization = Request.Headers.Authorization.ToString();

        var accessToken = await HttpContext.GetTokenAsync("access_token");
        var idToken = await HttpContext.GetTokenAsync("id_token");

        var claims = HttpContext.User?.Claims
            .Select(claim => new
            {
                claim.Type,
                claim.Value
            })
            .ToArray();

        return Ok(new
        {
            Authorization = string.IsNullOrWhiteSpace(authorization) ? null : authorization,
            AccessToken = string.IsNullOrWhiteSpace(accessToken) ? null : accessToken,
            IdToken = string.IsNullOrWhiteSpace(idToken) ? null : idToken,
            Claims = claims
        });
    }

    [HttpPut]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PutAsync(
        [FromBody] UpdateUserProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        try
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
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePasswordAsync(
        [FromBody] UpdateUserPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        try
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
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}
