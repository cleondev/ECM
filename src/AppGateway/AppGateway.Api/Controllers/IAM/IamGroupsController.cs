using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Api.Auth;
using AppGateway.Contracts.IAM.Groups;
using AppGateway.Infrastructure.Auth;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AppGateway.Api.Controllers.IAM;

[ApiController]
[Route("api/iam/groups")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class IamGroupsController(IEcmApiClient client, ILogger<IamGroupsController> logger) : ControllerBase
{
    private readonly IEcmApiClient _client = client;
    private readonly ILogger<IamGroupsController> _logger = logger;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<GroupSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var cachedProfile = PasswordLoginClaims.GetProfileFromPrincipal(User, out var invalidProfileClaim);
        if (cachedProfile is not null)
        {
            return Ok(
                cachedProfile.Groups is { Count: > 0 } cachedGroups
                    ? cachedGroups
                    : Array.Empty<GroupSummaryDto>());
        }

        if (invalidProfileClaim)
        {
            _logger.LogWarning(
                "Password login principal had an invalid stored profile while resolving /api/iam/groups. Signing out.");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Unauthorized();
        }

        var profile = await _client.GetCurrentUserProfileAsync(cancellationToken);
        if (profile is null)
        {
            return NotFound();
        }

        var groups = profile.Groups;
        if (groups is null || groups.Count == 0)
        {
            return Ok(Array.Empty<GroupSummaryDto>());
        }

        return Ok(groups);
    }
}
