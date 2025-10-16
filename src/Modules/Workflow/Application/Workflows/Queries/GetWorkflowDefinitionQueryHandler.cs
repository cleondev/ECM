using System.Threading;
using System.Threading.Tasks;
using ECM.Workflow.Domain.Definitions;

namespace ECM.Workflow.Application.Workflows.Queries;

public sealed class GetWorkflowDefinitionQueryHandler(IWorkflowRepository repository)
{
    private readonly IWorkflowRepository _repository = repository;

    public Task<WorkflowDefinition?> HandleAsync(
        GetWorkflowDefinitionQuery query,
        CancellationToken cancellationToken)
        => _repository.GetDefinitionByIdAsync(query.DefinitionId, cancellationToken);
}
