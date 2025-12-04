using AppGateway.Api.Auth;
using AppGateway.Api.Fake;
using AppGateway.Contracts.Workflows;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Api.Controllers;

[ApiController]
[Route("api/workflows")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class WorkflowsController(IWorkflowsApiClient client) : ControllerBase
{
    private readonly IWorkflowsApiClient _client = client;

    [HttpGet("instances")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ListAsync([FromQuery] Guid documentId)
    {
        if (documentId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(documentId), "A valid document identifier is required.");
            return ValidationProblem(ModelState);
        }

        var instances = FakeEcmData.GetWorkflowInstances(documentId);
        return Ok(new { items = instances });
    }

    [HttpGet("instances/{instanceId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetInstance(Guid instanceId, [FromQuery] Guid? documentId)
    {
        if (instanceId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(instanceId), "A valid workflow instance identifier is required.");
            return ValidationProblem(ModelState);
        }

        var instance = FakeEcmData.GetWorkflowInstance(instanceId, documentId);
        return instance is null ? NotFound() : Ok(instance);
    }

    [HttpPost("instances")]
    [ProducesResponseType(typeof(WorkflowInstanceDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartAsync([FromBody] StartWorkflowRequestDto request, CancellationToken cancellationToken)
    {
        var instance = await _client.StartWorkflowAsync(request, cancellationToken);
        if (instance is null)
        {
            return Problem(title: "Failed to start workflow", statusCode: StatusCodes.Status400BadRequest);
        }

        return Accepted(instance);
    }
}
