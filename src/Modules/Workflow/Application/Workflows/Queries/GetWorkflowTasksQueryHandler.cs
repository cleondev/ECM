using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.Workflow.Application.Workflows.Tasks;
using ECM.Workflow.Domain.Tasks;

namespace ECM.Workflow.Application.Workflows.Queries;

public sealed class GetWorkflowTasksQueryHandler(IWorkflowRepository repository)
{
    private readonly IWorkflowRepository _repository = repository;

    public Task<IReadOnlyCollection<WorkflowTask>> HandleAsync(
        GetWorkflowTasksQuery query,
        CancellationToken cancellationToken)
    {
        var filter = new WorkflowTaskQuery(query.AssigneeId, query.State, query.DocumentId);
        return _repository.GetTasksAsync(filter, cancellationToken);
    }
}
