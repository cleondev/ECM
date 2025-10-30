using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;

namespace ECM.Workflow.Application.Workflows.Commands;

public sealed class ClaimWorkflowTaskCommandHandler(IWorkflowRepository repository)
{
    private readonly IWorkflowRepository _repository = repository;

    public Task<OperationResult> HandleAsync(
        ClaimWorkflowTaskCommand command,
        CancellationToken cancellationToken)
        => _repository.ClaimTaskAsync(command.TaskId, command.AssigneeId, cancellationToken);
}
