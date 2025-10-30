using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Api.Auth;
using AppGateway.Contracts.Tags;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Api.Controllers;

[ApiController]
[Route("api/tags")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class TagsController(IEcmApiClient client) : ControllerBase
{
    private readonly IEcmApiClient _client = client;

    [HttpPost]
    [ProducesResponseType(typeof(TagLabelDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateTagRequestDto request, CancellationToken cancellationToken)
    {
        if (request.NamespaceId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(request.NamespaceId), "Namespace identifier is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Tag name is required.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var tag = await _client.CreateTagAsync(request, cancellationToken);
        if (tag is null)
        {
            return Problem(title: "Failed to create tag", statusCode: StatusCodes.Status400BadRequest);
        }

        return Created($"/api/tags/{tag.Id}", tag);
    }

    [HttpPut("{tagId:guid}")]
    [ProducesResponseType(typeof(TagLabelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(Guid tagId, [FromBody] UpdateTagRequestDto request, CancellationToken cancellationToken)
    {
        if (tagId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(tagId), "Tag identifier is required.");
        }

        if (request.NamespaceId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(request.NamespaceId), "Namespace identifier is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Tag name is required.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var tag = await _client.UpdateTagAsync(tagId, request, cancellationToken);
        if (tag is null)
        {
            return NotFound();
        }

        return Ok(tag);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<TagLabelDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var tags = await _client.GetTagsAsync(cancellationToken);
        return Ok(tags);
    }

    [HttpDelete("{tagId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAsync(Guid tagId, CancellationToken cancellationToken)
    {
        if (tagId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(tagId), "Tag identifier is required.");
            return ValidationProblem(ModelState);
        }

        var deleted = await _client.DeleteTagAsync(tagId, cancellationToken);
        if (!deleted)
        {
            return Problem(title: "Failed to delete tag", statusCode: StatusCodes.Status400BadRequest);
        }

        return NoContent();
    }
}
