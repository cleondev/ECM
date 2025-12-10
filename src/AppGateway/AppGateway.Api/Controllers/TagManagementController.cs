using AppGateway.Api.Auth;
using AppGateway.Contracts.Tags;
using AppGateway.Infrastructure.Ecm;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Api.Controllers;

[ApiController]
[Route("api/tag-management")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class TagManagementController(IEcmApiClient client) : ControllerBase
{
    private readonly IEcmApiClient _client = client;

    [HttpGet("tags")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TagLabelDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTagsAsync(
        [FromQuery] string? scope,
        [FromQuery(Name = "includeAll")] bool includeAllNamespaces,
        CancellationToken cancellationToken)
    {
        var tags = await _client.GetManagedTagsAsync(scope, includeAllNamespaces, cancellationToken);
        return Ok(tags);
    }

    [HttpGet("namespaces")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TagNamespaceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListNamespacesAsync(
        [FromQuery] string? scope,
        [FromQuery(Name = "includeAll")] bool includeAllNamespaces,
        CancellationToken cancellationToken)
    {
        var namespaces = await _client.GetTagNamespacesAsync(scope, includeAllNamespaces, cancellationToken);
        return Ok(namespaces);
    }

    [HttpPost("tags")]
    [ProducesResponseType(typeof(TagLabelDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTagAsync(
        [FromBody] ManagementCreateTagRequestDto request,
        CancellationToken cancellationToken)
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

        var tag = await _client.CreateManagedTagAsync(request, cancellationToken);
        if (tag is null)
        {
            return Problem(title: "Failed to create managed tag", statusCode: StatusCodes.Status400BadRequest);
        }

        return Created($"/api/tag-management/tags/{tag.Id}", tag);
    }

    [HttpPut("tags/{tagId:guid}")]
    [ProducesResponseType(typeof(TagLabelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTagAsync(
        Guid tagId,
        [FromBody] ManagementUpdateTagRequestDto request,
        CancellationToken cancellationToken)
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

        var tag = await _client.UpdateManagedTagAsync(tagId, request, cancellationToken);
        if (tag is null)
        {
            return NotFound();
        }

        return Ok(tag);
    }

    [HttpDelete("tags/{tagId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteTagAsync(Guid tagId, CancellationToken cancellationToken)
    {
        if (tagId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(tagId), "Tag identifier is required.");
            return ValidationProblem(ModelState);
        }

        var deleted = await _client.DeleteManagedTagAsync(tagId, cancellationToken);
        if (!deleted)
        {
            return Problem(title: "Failed to delete managed tag", statusCode: StatusCodes.Status400BadRequest);
        }

        return NoContent();
    }

    [HttpPost("namespaces")]
    [ProducesResponseType(typeof(TagNamespaceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateNamespaceAsync(
        [FromBody] CreateTagNamespaceRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Scope))
        {
            ModelState.AddModelError(nameof(request.Scope), "Scope is required.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var created = await _client.CreateTagNamespaceAsync(request, cancellationToken);
        if (created is null)
        {
            return Problem(title: "Failed to create tag namespace", statusCode: StatusCodes.Status400BadRequest);
        }

        return Created($"/api/tag-management/namespaces/{created.Id}", created);
    }

    [HttpPut("namespaces/{namespaceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNamespaceAsync(
        Guid namespaceId,
        [FromBody] UpdateTagNamespaceRequestDto request,
        CancellationToken cancellationToken)
    {
        if (namespaceId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(namespaceId), "Namespace identifier is required.");
            return ValidationProblem(ModelState);
        }

        var updated = await _client.UpdateTagNamespaceAsync(namespaceId, request, cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("namespaces/{namespaceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteNamespaceAsync(Guid namespaceId, CancellationToken cancellationToken)
    {
        if (namespaceId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(namespaceId), "Namespace identifier is required.");
            return ValidationProblem(ModelState);
        }

        var deleted = await _client.DeleteTagNamespaceAsync(namespaceId, cancellationToken);
        if (!deleted)
        {
            return Problem(title: "Failed to delete tag namespace", statusCode: StatusCodes.Status400BadRequest);
        }

        return NoContent();
    }
}
