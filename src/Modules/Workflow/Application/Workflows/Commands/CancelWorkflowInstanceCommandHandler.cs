using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;

namespace ECM.Workflow.Application.Workflows.Commands;

public sealed class CancelWorkflowInstanceCommandHandler(IWorkflowRepository repository)
{
    private readonly IWorkflowRepository _repository = repository;

    public Task<OperationResult> HandleAsync(
        CancelWorkflowInstanceCommand command,
        CancellationToken cancellationToken)
        => _repository.CancelInstanceAsync(command.InstanceId, command.Reason, cancellationToken);
}
