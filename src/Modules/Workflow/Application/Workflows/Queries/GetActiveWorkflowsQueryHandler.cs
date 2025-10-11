using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.Workflow.Application.Workflows;
using ECM.Workflow.Domain.Instances;

namespace ECM.Workflow.Application.Workflows.Queries;

public sealed class GetActiveWorkflowsQueryHandler(IWorkflowRepository repository)
{
    private readonly IWorkflowRepository _repository = repository;

    public Task<IReadOnlyCollection<WorkflowInstance>> HandleAsync(GetActiveWorkflowsQuery query, CancellationToken cancellationToken)
        => _repository.GetActiveAsync(cancellationToken);
}
