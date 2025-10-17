using ECM.BuildingBlocks.Application;
using ECM.Workflow.Application.Workflows.Commands;
using ECM.Workflow.Application.Workflows.Tasks;
using ECM.Workflow.Domain.Definitions;
using ECM.Workflow.Domain.Instances;
using ECM.Workflow.Domain.Tasks;

namespace ECM.Workflow.Application.Workflows;

public interface IWorkflowRepository
{
    Task<OperationResult<WorkflowInstance>> StartAsync(Guid documentId, string definitionKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorkflowInstance>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorkflowDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default);

    Task<WorkflowDefinition?> GetDefinitionByIdAsync(string definitionId, CancellationToken cancellationToken = default);

    Task<WorkflowInstance?> GetInstanceByExternalIdAsync(string instanceId, CancellationToken cancellationToken = default);

    Task<OperationResult> CancelInstanceAsync(string instanceId, string? reason, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorkflowTask>> GetTasksAsync(WorkflowTaskQuery query, CancellationToken cancellationToken = default);

    Task<WorkflowTask?> GetTaskAsync(string taskId, CancellationToken cancellationToken = default);

    Task<OperationResult> ClaimTaskAsync(string taskId, string assigneeId, CancellationToken cancellationToken = default);

    Task<OperationResult> CompleteTaskAsync(CompleteWorkflowTaskCommand command, CancellationToken cancellationToken = default);

    Task<OperationResult> ReassignTaskAsync(string taskId, string assigneeId, CancellationToken cancellationToken = default);
}
