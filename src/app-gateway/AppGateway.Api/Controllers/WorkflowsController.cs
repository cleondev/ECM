using AppGateway.Contracts.Workflows;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Api.Controllers;

[ApiController]
[Route("api/gateway/workflows")]
[Authorize]
public sealed class WorkflowsController(IEcmApiClient client) : ControllerBase
{
    private readonly IEcmApiClient _client = client;

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
