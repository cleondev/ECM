using System.Threading;
using System.Threading.Tasks;
using AppGateway.Api.Auth;
using AppGateway.Contracts.IAM.Users;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AppGateway.Api.Controllers.IAM;

[ApiController]
[Route("api/iam/profile")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class IamUserProfileController(
    IEcmApiClient client,
    ILogger<IamUserProfileController> logger) : ControllerBase
{
    private readonly IEcmApiClient _client = client;
    private readonly ILogger<IamUserProfileController> _logger = logger;

    [HttpGet]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var cachedProfile = PasswordLoginClaims.GetProfileFromPrincipal(User, out var invalidProfileClaim);
        if (cachedProfile is not null)
        {
            return Ok(cachedProfile);
        }

        if (invalidProfileClaim)
        {
            _logger.LogWarning(
                "Password login principal had an invalid stored profile while resolving /api/iam/profile. Signing out.");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Unauthorized();
        }

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
