using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Contracts.IAM.Relations;
using AppGateway.Api.Auth;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Api.Controllers.IAM;

[ApiController]
[Route("api/iam/relations")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class IamRelationsController(IRelationsApiClient client) : ControllerBase
{
    private readonly IRelationsApiClient _client = client;

    [HttpGet("subjects/{subjectType}/{subjectId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AccessRelationDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<AccessRelationDto>> GetBySubjectAsync(string subjectType, Guid subjectId, CancellationToken cancellationToken)
        => await _client.GetRelationsBySubjectAsync(subjectType, subjectId, cancellationToken);

    [HttpGet("objects/{objectType}/{objectId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AccessRelationDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyCollection<AccessRelationDto>> GetByObjectAsync(string objectType, Guid objectId, CancellationToken cancellationToken)
        => await _client.GetRelationsByObjectAsync(objectType, objectId, cancellationToken);

    [HttpPost]
    [ProducesResponseType(typeof(AccessRelationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostAsync([FromBody] CreateAccessRelationRequestDto request, CancellationToken cancellationToken)
    {
        var relation = await _client.CreateRelationAsync(request, cancellationToken);
        if (relation is null)
        {
            return Problem(title: "Failed to create relation", statusCode: StatusCodes.Status400BadRequest);
        }

        return CreatedAtAction(nameof(GetBySubjectAsync), new { subjectType = relation.SubjectType, subjectId = relation.SubjectId }, relation);
    }

    [HttpDelete("subjects/{subjectType}/{subjectId:guid}/objects/{objectType}/{objectId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(string subjectType, Guid subjectId, string objectType, Guid objectId, [FromQuery] string relation, CancellationToken cancellationToken)
    {
        var deleted = await _client.DeleteRelationAsync(subjectType, subjectId, objectType, objectId, relation, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
