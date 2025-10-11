using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ECM.Workflow.Application.Workflows.Commands;
using ECM.Workflow.Application.Workflows.Queries;
using ECM.Workflow.Domain.Instances;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace ECM.Workflow.Api.Workflows;

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

    private static async Task<Ok<IReadOnlyCollection<WorkflowInstance>>> GetActiveWorkflows(GetActiveWorkflowsQueryHandler handler, CancellationToken cancellationToken)
    {
        var instances = await handler.HandleAsync(new GetActiveWorkflowsQuery(), cancellationToken);
        return TypedResults.Ok(instances);
    }

    private static async Task<Results<Accepted<WorkflowInstance>, ValidationProblem>> StartWorkflow(StartWorkflowRequest request, StartWorkflowCommandHandler handler, CancellationToken cancellationToken)
    {
        var command = new StartWorkflowCommand(request.DocumentId, request.Definition);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["workflow"] = [.. result.Errors]
            });
        }

        return TypedResults.Accepted($"/api/ecm/workflows/instances/{result.Value.Id}", result.Value);
    }
}
