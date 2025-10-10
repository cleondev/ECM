using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ECM.Modules.Workflow.Application.Workflows;
using ECM.Modules.Workflow.Domain.Instances;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace ECM.Modules.Workflow.Api.Workflows;

public static class WorkflowEndpoints
{
    public static RouteGroupBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/ecm/workflows");
        group.WithTags("Workflow");

        group.MapGet("/instances", GetActiveWorkflows)
             .WithName("GetActiveWorkflows");

        group.MapPost("/instances", StartWorkflow)
             .WithName("StartWorkflow");

        return group;
    }

    private static async Task<Ok<IReadOnlyCollection<WorkflowInstance>>> GetActiveWorkflows(WorkflowApplicationService service, CancellationToken cancellationToken)
    {
        var instances = await service.GetActiveAsync(cancellationToken);
        return TypedResults.Ok(instances);
    }

    private static async Task<Results<Accepted<WorkflowInstance>, ValidationProblem>> StartWorkflow(StartWorkflowRequest request, WorkflowApplicationService service, CancellationToken cancellationToken)
    {
        var command = new StartWorkflowCommand(request.DocumentId, request.Definition);
        var result = await service.StartAsync(command, cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            var errors = new Dictionary<string, string[]>
            {
                ["workflow"] = result.Errors.ToArray()
            };

            return TypedResults.ValidationProblem(errors);
        }

        return TypedResults.Accepted($"/api/ecm/workflows/instances/{result.Value.Id}", result.Value);
    }
}
