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
    public async Task<IReadOnlyCollection<GroupSummaryDto>> GetAsync(CancellationToken cancellationToken)
        => await _client.GetGroupsAsync(cancellationToken);

    [HttpPost]
    [ProducesResponseType(typeof(GroupSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostAsync([FromBody] CreateGroupRequestDto request, CancellationToken cancellationToken)
    {
        var group = await _client.CreateGroupAsync(request, cancellationToken);
        if (group is null)
        {
            return Problem(title: "Failed to create group", statusCode: StatusCodes.Status400BadRequest);
        }

        return CreatedAtAction(nameof(GetAsync), new { id = group.Id }, group);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GroupSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PutAsync(Guid id, [FromBody] UpdateGroupRequestDto request, CancellationToken cancellationToken)
    {
        var group = await _client.UpdateGroupAsync(id, request, cancellationToken);
        if (group is null)
        {
            return NotFound();
        }

        return Ok(group);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _client.DeleteGroupAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
