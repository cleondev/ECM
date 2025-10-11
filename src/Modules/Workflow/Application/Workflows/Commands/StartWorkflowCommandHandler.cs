using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.Workflow.Application.Workflows;
using ECM.Workflow.Domain.Definitions;
using ECM.Workflow.Domain.Instances;

namespace ECM.Workflow.Application.Workflows.Commands;

public sealed class StartWorkflowCommandHandler(IWorkflowRepository repository)
{
    private readonly IWorkflowRepository _repository = repository;
    private static readonly WorkflowDefinition DefaultDefinition = new(Guid.Parse("00000000-0000-0000-0000-000000000001"), "default-approval", new[] { "prepare", "approve", "archive" });

    public async Task<OperationResult<WorkflowInstance>> HandleAsync(StartWorkflowCommand command, CancellationToken cancellationToken)
    {
        var result = command.ToDomain(DefaultDefinition);
        if (result.IsFailure || result.Value is null)
        {
            return result;
        }

        await _repository.AddAsync(result.Value, cancellationToken);
        return result;
    }
}
