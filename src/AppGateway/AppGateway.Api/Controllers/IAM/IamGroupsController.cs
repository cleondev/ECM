using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Api.Auth;
using AppGateway.Contracts.IAM.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Api.Controllers.IAM;

[ApiController]
[Route("api/iam/groups")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
// API yêu cầu thông tin profile nên dùng filter để tự động đồng bộ/truy xuất.
[RequireCurrentUserProfile]
public sealed class IamGroupsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<GroupSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Get()
    {
        if (!CurrentUserProfileStore.TryGet(HttpContext, out var profile) || profile is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        var groups = profile.Groups;
        if (groups is null || groups.Count == 0)
        {
            return Ok(Array.Empty<GroupSummaryDto>());
        }

        return Ok(groups);
    }
}
