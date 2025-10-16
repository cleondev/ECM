using System.Threading;
using System.Threading.Tasks;
using ECM.Workflow.Domain.Tasks;

namespace ECM.Workflow.Application.Workflows.Queries;

public sealed class GetWorkflowTaskQueryHandler(IWorkflowRepository repository)
{
    private readonly IWorkflowRepository _repository = repository;

    public Task<WorkflowTask?> HandleAsync(
        GetWorkflowTaskQuery query,
        CancellationToken cancellationToken)
        => _repository.GetTaskAsync(query.TaskId, cancellationToken);
}
