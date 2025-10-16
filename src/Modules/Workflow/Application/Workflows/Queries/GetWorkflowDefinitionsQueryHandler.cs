using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.Workflow.Domain.Definitions;

namespace ECM.Workflow.Application.Workflows.Queries;

public sealed class GetWorkflowDefinitionsQueryHandler(IWorkflowRepository repository)
{
    private readonly IWorkflowRepository _repository = repository;

    public Task<IReadOnlyCollection<WorkflowDefinition>> HandleAsync(
        GetWorkflowDefinitionsQuery query,
        CancellationToken cancellationToken)
        => _repository.GetDefinitionsAsync(cancellationToken);
}
