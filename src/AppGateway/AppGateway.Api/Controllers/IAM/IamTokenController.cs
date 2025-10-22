using System;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Api.Auth;
using AppGateway.Contracts.IAM.Tokens;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace AppGateway.Api.Controllers.IAM;

[ApiController]
[Route("api/iam/tokens")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.AuthenticationScheme)]
public sealed class IamTokenController : ControllerBase
{
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly ILogger<IamTokenController> _logger;
    private readonly string? _appScope;

    public IamTokenController(
        ITokenAcquisition tokenAcquisition,
        IConfiguration configuration,
        ILogger<IamTokenController> logger)
    {
        _tokenAcquisition = tokenAcquisition;
        _logger = logger;

        var configuredScopes = ScopeUtilities.ParseScopes(
            configuration.GetValue<string>("Services:EcmScope"));

        _appScope = ScopeUtilities.TryGetAppScope(configuredScopes);
    }

    [HttpPost("app")]
    [ProducesResponseType(typeof(AccessTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AcquireAppTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_appScope))
        {
            _logger.LogError("Unable to acquire app token because the ECM scope is not configured with a default scope.");
            return Problem(
                title: "ECM scope not configured.",
                detail: "The Services:EcmScope configuration must include an app default scope (ending with '/.default').",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        try
        {
            var result = await _tokenAcquisition.GetAuthenticationResultForAppAsync(
                new[] { _appScope },
                cancellationToken: cancellationToken);

            return Ok(new AccessTokenResponseDto(result.AccessToken, result.ExpiresOn));
        }
        catch (MsalServiceException ex)
        {
            _logger.LogError(ex, "Azure AD rejected the client credentials request for the ECM scope {Scope}.", _appScope);
            return Problem(
                title: "Failed to acquire token.",
                detail: "Azure AD rejected the client credentials request. Verify the gateway app registration has API permissions to access the ECM host.",
                statusCode: StatusCodes.Status502BadGateway);
        }
        catch (MsalClientException ex)
        {
            _logger.LogError(ex, "The client credentials request failed for the ECM scope {Scope}.", _appScope);
            return Problem(
                title: "Failed to acquire token.",
                detail: "The request to Azure AD failed. Check the gateway's Azure AD configuration (ClientId/ClientSecret).",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
