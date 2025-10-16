using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;

namespace ECM.Workflow.Application.Workflows.Commands;

public sealed class ReassignWorkflowTaskCommandHandler(IWorkflowRepository repository)
{
    private readonly IWorkflowRepository _repository = repository;

    public Task<OperationResult> HandleAsync(
        ReassignWorkflowTaskCommand command,
        CancellationToken cancellationToken)
        => _repository.ReassignTaskAsync(command.TaskId, command.AssigneeId, cancellationToken);
}
