using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.Workflow.Application.Workflows;
using ECM.Workflow.Domain.Definitions;
using ECM.Workflow.Domain.Instances;

namespace ECM.Workflow.Infrastructure.Instances;

internal sealed class InMemoryWorkflowRepository : IWorkflowRepository
{
    private readonly ConcurrentDictionary<Guid, WorkflowInstance> _instances = new();

    public Task<OperationResult<WorkflowInstance>> StartAsync(Guid documentId, string definitionKey, CancellationToken cancellationToken = default)
    {
        var definition = new WorkflowDefinition("in-memory", definitionKey, definitionKey, 1);
        var instance = new WorkflowInstance(Guid.NewGuid(), documentId, definition, WorkflowStatus.Running, DateTimeOffset.UtcNow, Guid.NewGuid().ToString());
        _instances[instance.Id] = instance;
        return Task.FromResult(OperationResult<WorkflowInstance>.Success(instance));
    }

    public Task<IReadOnlyCollection<WorkflowInstance>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var active = _instances.Values
            .Where(instance => instance.Status is WorkflowStatus.Running)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<WorkflowInstance>>(active);
    }
}
