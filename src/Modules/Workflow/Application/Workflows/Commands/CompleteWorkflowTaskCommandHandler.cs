using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;

namespace ECM.Workflow.Application.Workflows.Commands;

public sealed class CompleteWorkflowTaskCommandHandler(IWorkflowRepository repository)
{
    private readonly IWorkflowRepository _repository = repository;

    public Task<OperationResult> HandleAsync(
        CompleteWorkflowTaskCommand command,
        CancellationToken cancellationToken)
        => _repository.CompleteTaskAsync(command, cancellationToken);
}
