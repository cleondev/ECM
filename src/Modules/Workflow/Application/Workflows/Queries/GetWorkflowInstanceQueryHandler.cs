using System.Threading;
using System.Threading.Tasks;
using ECM.Workflow.Domain.Instances;

namespace ECM.Workflow.Application.Workflows.Queries;

public sealed class GetWorkflowInstanceQueryHandler(IWorkflowRepository repository)
{
    private readonly IWorkflowRepository _repository = repository;

    public Task<WorkflowInstance?> HandleAsync(
        GetWorkflowInstanceQuery query,
        CancellationToken cancellationToken)
        => _repository.GetInstanceByExternalIdAsync(query.InstanceId, cancellationToken);
}
