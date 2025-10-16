using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Contracts.IAM.Roles;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Api.Controllers.IAM;

[ApiController]
[Route("api/gateway/iam/roles")]
[Authorize]
public sealed class IamRolesController(IEcmApiClient client) : ControllerBase
{
    private readonly IEcmApiClient _client = client;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<RoleSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<RoleSummaryDto>> GetAsync(CancellationToken cancellationToken)
        => await _client.GetRolesAsync(cancellationToken);

    [HttpPost]
    [ProducesResponseType(typeof(RoleSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostAsync([FromBody] CreateRoleRequestDto request, CancellationToken cancellationToken)
    {
        var role = await _client.CreateRoleAsync(request, cancellationToken);
        if (role is null)
        {
            return Problem(title: "Failed to create role", statusCode: StatusCodes.Status400BadRequest);
        }

        return Created($"api/gateway/access-control/roles/{role.Id}", role);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RoleSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PutAsync(Guid id, [FromBody] RenameRoleRequestDto request, CancellationToken cancellationToken)
    {
        var role = await _client.RenameRoleAsync(id, request, cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        return Ok(role);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _client.DeleteRoleAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
