using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Contracts.IAM.Users;
using AppGateway.Api.Auth;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Api.Controllers.IAM;

[ApiController]
[Route("api/iam/users")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class IamUsersController(IEcmApiClient client) : ControllerBase
{
    private readonly IEcmApiClient _client = client;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<UserSummaryDto>> GetAsync(CancellationToken cancellationToken)
        => await _client.GetUsersAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _client.GetUserAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostAsync([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _client.CreateUserAsync(request, cancellationToken);
        if (user is null)
        {
            return Problem(title: "Failed to create user", statusCode: StatusCodes.Status400BadRequest);
        }

        return CreatedAtAction(nameof(GetByIdAsync), new { id = user.Id }, user);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PutAsync(Guid id, [FromBody] UpdateUserRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _client.UpdateUserAsync(id, request, cancellationToken);
        if (user is null)
        {
            var existing = await _client.GetUserAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            return Problem(title: "Failed to update user", statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(user);
    }

    [HttpPost("{id:guid}/roles")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRoleAsync(Guid id, [FromBody] AssignRoleRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _client.AssignRoleToUserAsync(id, request, cancellationToken);
        if (user is null)
        {
            var existing = await _client.GetUserAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            return Problem(title: "Failed to assign role", statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(user);
    }

    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRoleAsync(Guid id, Guid roleId, CancellationToken cancellationToken)
    {
        var user = await _client.RemoveRoleFromUserAsync(id, roleId, cancellationToken);
        if (user is null)
        {
            var existing = await _client.GetUserAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            return Problem(title: "Failed to remove role", statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(user);
    }
}
