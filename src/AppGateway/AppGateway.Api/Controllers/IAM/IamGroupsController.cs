using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Api.Auth;
using AppGateway.Contracts.IAM.Groups;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Api.Controllers.IAM;

[ApiController]
[Route("api/iam/groups")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class IamGroupsController(IEcmApiClient client) : ControllerBase
{
    private readonly IEcmApiClient _client = client;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<GroupSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
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
