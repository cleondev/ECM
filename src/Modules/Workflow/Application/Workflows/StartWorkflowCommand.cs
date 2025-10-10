using System;
using ECM.BuildingBlocks.Application;
using ECM.Workflow.Domain.Definitions;
using ECM.Workflow.Domain.Instances;

namespace ECM.Workflow.Application.Workflows;

public sealed record StartWorkflowCommand(Guid DocumentId, string Definition)
{
    public OperationResult<WorkflowInstance> ToDomain(WorkflowDefinition definition)
    {
        if (!string.Equals(Definition, definition.Name, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<WorkflowInstance>.Failure("Unknown workflow definition");
        }

        var instance = new WorkflowInstance(Guid.NewGuid(), DocumentId, definition, WorkflowStatus.Running, DateTimeOffset.UtcNow);
        return OperationResult<WorkflowInstance>.Success(instance);
    }
}
