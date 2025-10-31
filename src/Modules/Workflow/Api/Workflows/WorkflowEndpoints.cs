using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ECM.Workflow.Api;
using ECM.Workflow.Application.Workflows.Commands;
using ECM.Workflow.Application.Workflows.Queries;
using ECM.Workflow.Domain.Definitions;
using ECM.Workflow.Domain.Instances;
using ECM.Workflow.Domain.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ECM.Workflow.Api.Workflows;

public static class WorkflowEndpoints
{
    public static RouteGroupBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/ecm/wf");
        group.WithTags("Workflow");
        group.WithGroupName(WorkflowSwagger.DocumentName);

        var definitions = group.MapGroup("/definitions");
        definitions.MapGet("/", GetDefinitions)
                   .WithName("GetWorkflowDefinitions");
        definitions.MapGet("/{definitionId}", GetDefinition)
                   .WithName("GetWorkflowDefinitionById");

        var instances = group.MapGroup("/instances");
        instances.MapGet("/", GetInstances)
                 .WithName("GetWorkflowInstances");
        instances.MapPost("/", StartWorkflow)
                 .WithName("StartWorkflowInstance");
        instances.MapGet("/{instanceId}", GetInstance)
                 .WithName("GetWorkflowInstance");
        instances.MapPost("/{instanceId}/cancel", CancelInstance)
                 .WithName("CancelWorkflowInstance");

        var tasks = group.MapGroup("/tasks");
        tasks.MapGet("/", GetTasks)
             .WithName("GetWorkflowTasks");
        tasks.MapGet("/{taskId}", GetTask)
             .WithName("GetWorkflowTask");
        tasks.MapPost("/{taskId}/claim", ClaimTask)
             .WithName("ClaimWorkflowTask");
        tasks.MapPost("/{taskId}/complete", CompleteTask)
             .WithName("CompleteWorkflowTask");
        tasks.MapPost("/{taskId}/reassign", ReassignTask)
             .WithName("ReassignWorkflowTask");

        return group;
    }

    private static async Task<Ok<IReadOnlyCollection<WorkflowDefinition>>> GetDefinitions(
        GetWorkflowDefinitionsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var definitions = await handler.HandleAsync(new GetWorkflowDefinitionsQuery(), cancellationToken);
        return TypedResults.Ok(definitions);
    }

    private static async Task<Results<Ok<WorkflowDefinition>, NotFound>> GetDefinition(
        string definitionId,
        GetWorkflowDefinitionQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var definition = await handler.HandleAsync(new GetWorkflowDefinitionQuery(definitionId), cancellationToken);
        return definition is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(definition);
    }

    private static async Task<Ok<IReadOnlyCollection<WorkflowInstance>>> GetInstances(
        [AsParameters] WorkflowInstanceListRequest request,
        GetActiveWorkflowsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var instances = await handler.HandleAsync(new GetActiveWorkflowsQuery(), cancellationToken);

        if (request.DocumentId.HasValue)
        {
            instances = [.. instances.Where(instance => instance.DocumentId == request.DocumentId)];
        }

        if (!string.IsNullOrWhiteSpace(request.State) && !request.State.Equals("open", System.StringComparison.OrdinalIgnoreCase))
        {
            instances = [];
        }

        return TypedResults.Ok(instances);
    }

    private static async Task<Results<Accepted<WorkflowInstance>, ValidationProblem>> StartWorkflow(
        StartWorkflowRequest request,
        StartWorkflowCommandHandler handler,
        CancellationToken cancellationToken)
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

        return TypedResults.Accepted($"/api/ecm/wf/instances/{result.Value.ExternalId}", result.Value);
    }

    private static async Task<Results<Ok<WorkflowInstance>, NotFound>> GetInstance(
        string instanceId,
        GetWorkflowInstanceQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var instance = await handler.HandleAsync(new GetWorkflowInstanceQuery(instanceId), cancellationToken);
        return instance is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(instance);
    }

    private static async Task<Results<NoContent, ValidationProblem>> CancelInstance(
        string instanceId,
        CancelWorkflowInstanceRequest request,
        CancelWorkflowInstanceCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CancelWorkflowInstanceCommand(instanceId, request.Reason);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["workflow"] = [result.Error]
            });
        }

        return TypedResults.NoContent();
    }

    private static async Task<Ok<IReadOnlyCollection<WorkflowTask>>> GetTasks(
        [AsParameters] WorkflowTaskListRequest request,
        GetWorkflowTasksQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetWorkflowTasksQuery(request.AssigneeId, request.State, request.DocumentId);
        var tasks = await handler.HandleAsync(query, cancellationToken);
        return TypedResults.Ok(tasks);
    }

    private static async Task<Results<Ok<WorkflowTask>, NotFound>> GetTask(
        string taskId,
        GetWorkflowTaskQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var task = await handler.HandleAsync(new GetWorkflowTaskQuery(taskId), cancellationToken);
        return task is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(task);
    }

    private static async Task<Results<NoContent, ValidationProblem>> ClaimTask(
        string taskId,
        ClaimWorkflowTaskRequest request,
        ClaimWorkflowTaskCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new ClaimWorkflowTaskCommand(taskId, request.AssigneeId);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["task"] = [result.Error]
            });
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, ValidationProblem>> CompleteTask(
        string taskId,
        CompleteWorkflowTaskRequest request,
        CompleteWorkflowTaskCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CompleteWorkflowTaskCommand(taskId, request.Action, request.Comment, request.Outputs);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["task"] = [result.Error]
            });
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, ValidationProblem>> ReassignTask(
        string taskId,
        ReassignWorkflowTaskRequest request,
        ReassignWorkflowTaskCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new ReassignWorkflowTaskCommand(taskId, request.AssigneeId);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["task"] = [result.Error]
            });
        }

        return TypedResults.NoContent();
    }
}
