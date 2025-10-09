using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.Modules.Workflow.Domain.Definitions;
using ECM.Modules.Workflow.Domain.Instances;

namespace ECM.Modules.Workflow.Application.Workflows;

public sealed class WorkflowApplicationService(IWorkflowRepository repository)
{
    private readonly IWorkflowRepository _repository = repository;
    private static readonly WorkflowDefinition DefaultDefinition = new(Guid.Parse("00000000-0000-0000-0000-000000000001"), "default-approval", new[] { "prepare", "approve", "archive" });

    public async Task<OperationResult<WorkflowInstance>> StartAsync(StartWorkflowCommand command, CancellationToken cancellationToken)
    {
        var result = command.ToDomain(DefaultDefinition);
        if (result.IsFailure || result.Value is null)
        {
            return result;
        }

        await _repository.AddAsync(result.Value, cancellationToken);
        return result;
    }

    public Task<IReadOnlyCollection<WorkflowInstance>> GetActiveAsync(CancellationToken cancellationToken)
        => _repository.GetActiveAsync(cancellationToken);
}
