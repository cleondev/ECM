using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.Workflow.Application.Workflows;
using ECM.Workflow.Domain.Instances;

namespace ECM.Workflow.Infrastructure.Instances;

internal sealed class InMemoryWorkflowRepository : IWorkflowRepository
{
    private readonly ConcurrentDictionary<Guid, WorkflowInstance> _instances = new();

    public Task AddAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        _instances[instance.Id] = instance;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<WorkflowInstance>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var active = _instances.Values
            .Where(instance => instance.Status is WorkflowStatus.Running)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<WorkflowInstance>>(active);
    }
}
