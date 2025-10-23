using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Api.Auth;
using AppGateway.Contracts.IAM.Users;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AppGateway.Api.Controllers.IAM;

[ApiController]
[Route("api/iam")]
public sealed class IamAuthenticationController(
    IEcmApiClient client,
    IUserProvisioningService provisioningService,
    ILogger<IamAuthenticationController> logger) : ControllerBase
{
    private readonly IEcmApiClient _client = client;
    private readonly IUserProvisioningService _provisioningService = provisioningService;
    private readonly ILogger<IamAuthenticationController> _logger = logger;

    [HttpGet("check-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CheckLoginResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckLoginAsync(
        [FromQuery] string? redirectUri,
        CancellationToken cancellationToken)
    {
        var resolvedRedirect = AzureLoginRedirectHelper.ResolveRedirectPath(
            HttpContext,
            redirectUri,
            Program.MainAppPath);

        var loginUrl = AzureLoginRedirectHelper.CreateLoginUrl(HttpContext, resolvedRedirect);

        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new CheckLoginResponseDto(false, resolvedRedirect, loginUrl, null));
        }

        try
        {
            var provisionedProfile = await _provisioningService.EnsureUserExistsAsync(User, cancellationToken);
            var profile = provisionedProfile ?? await _client.GetCurrentUserProfileAsync(cancellationToken);

            if (profile is null)
            {
                _logger.LogWarning("Authenticated principal did not resolve to a user profile.");
                return Ok(new CheckLoginResponseDto(true, resolvedRedirect, null, null));
            }

            return Ok(new CheckLoginResponseDto(true, resolvedRedirect, null, profile));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "An error occurred while checking the current user's login state.");
            return Ok(new CheckLoginResponseDto(false, resolvedRedirect, loginUrl, null));
        }
    }
}
