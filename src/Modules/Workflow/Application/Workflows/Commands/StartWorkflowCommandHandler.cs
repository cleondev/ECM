using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.Workflow.Application.Workflows;
using ECM.Workflow.Domain.Instances;

namespace ECM.Workflow.Application.Workflows.Commands;

public sealed class StartWorkflowCommandHandler(IWorkflowRepository repository)
{
    private readonly IWorkflowRepository _repository = repository;

    public async Task<OperationResult<WorkflowInstance>> HandleAsync(StartWorkflowCommand command, CancellationToken cancellationToken)
    {
        return await _repository.StartAsync(command.DocumentId, command.Definition, cancellationToken);
    }
}
