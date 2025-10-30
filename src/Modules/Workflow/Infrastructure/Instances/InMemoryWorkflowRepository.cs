using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.Workflow.Application.Workflows;
using ECM.Workflow.Application.Workflows.Commands;
using ECM.Workflow.Application.Workflows.Tasks;
using ECM.Workflow.Domain.Definitions;
using ECM.Workflow.Domain.Instances;
using ECM.Workflow.Domain.Tasks;

namespace ECM.Workflow.Infrastructure.Instances;

internal sealed class InMemoryWorkflowRepository : IWorkflowRepository
{
    private readonly ConcurrentDictionary<string, WorkflowInstance> _instances = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, WorkflowDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, WorkflowTask> _tasks = new(StringComparer.OrdinalIgnoreCase);

    public Task<OperationResult<WorkflowInstance>> StartAsync(Guid documentId, string definitionKey, CancellationToken cancellationToken = default)
    {
        var definition = _definitions.GetOrAdd(definitionKey, key => new WorkflowDefinition(Guid.NewGuid().ToString(), key, key, 1));
        var instance = new WorkflowInstance(Guid.NewGuid(), documentId, definition, WorkflowStatus.Running, DateTimeOffset.UtcNow, Guid.NewGuid().ToString());
        _instances[instance.ExternalId] = instance;
        return Task.FromResult(OperationResult<WorkflowInstance>.Success(instance));
    }

    public Task<IReadOnlyCollection<WorkflowInstance>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var active = _instances.Values
            .Where(instance => instance.Status is WorkflowStatus.Running)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<WorkflowInstance>>(active);
    }

    public Task<IReadOnlyCollection<WorkflowDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<WorkflowDefinition>>(_definitions.Values.ToArray());

    public Task<WorkflowDefinition?> GetDefinitionByIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var definition = _definitions.Values.FirstOrDefault(def => string.Equals(def.Id, definitionId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(definition);
    }

    public Task<WorkflowInstance?> GetInstanceByExternalIdAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        _instances.TryGetValue(instanceId, out var instance);
        return Task.FromResult(instance);
    }

    public Task<OperationResult> CancelInstanceAsync(string instanceId, string? reason, CancellationToken cancellationToken = default)
    {
        if (!_instances.TryGetValue(instanceId, out var instance))
        {
            return Task.FromResult(OperationResult.Failure("Workflow instance not found."));
        }

        instance.MarkCancelled(DateTimeOffset.UtcNow);
        return Task.FromResult(OperationResult.Success());
    }

    public Task<IReadOnlyCollection<WorkflowTask>> GetTasksAsync(WorkflowTaskQuery query, CancellationToken cancellationToken = default)
    {
        var tasks = _tasks.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.AssigneeId))
        {
            tasks = tasks.Where(task => string.Equals(task.AssigneeId, query.AssigneeId, StringComparison.OrdinalIgnoreCase));
        }

        if (query.DocumentId.HasValue)
        {
            tasks = tasks.Where(task => task.DocumentId == query.DocumentId);
        }

        if (!string.IsNullOrWhiteSpace(query.State) && Enum.TryParse<WorkflowTaskState>(query.State, true, out var state))
        {
            tasks = tasks.Where(task => task.State == state);
        }

        return Task.FromResult<IReadOnlyCollection<WorkflowTask>>(tasks.ToArray());
    }

    public Task<WorkflowTask?> GetTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        _tasks.TryGetValue(taskId, out var task);
        return Task.FromResult(task);
    }

    public Task<OperationResult> ClaimTaskAsync(string taskId, string assigneeId, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            return Task.FromResult(OperationResult.Failure("Task not found."));
        }

        task.Assign(assigneeId);
        return Task.FromResult(OperationResult.Success());
    }

    public Task<OperationResult> CompleteTaskAsync(CompleteWorkflowTaskCommand command, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(command.TaskId, out var task))
        {
            return Task.FromResult(OperationResult.Failure("Task not found."));
        }

        task.MarkCompleted();
        if (command.Outputs is not null)
        {
            task.UpdateVariables(command.Outputs);
        }

        return Task.FromResult(OperationResult.Success());
    }

    public Task<OperationResult> ReassignTaskAsync(string taskId, string assigneeId, CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            return Task.FromResult(OperationResult.Failure("Task not found."));
        }

        task.Assign(assigneeId);
        return Task.FromResult(OperationResult.Success());
    }

}
