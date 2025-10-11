using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.Workflow.Domain.Instances;

namespace ECM.Workflow.Application.Workflows;

public interface IWorkflowRepository
{
    Task AddAsync(WorkflowInstance instance, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorkflowInstance>> GetActiveAsync(CancellationToken cancellationToken = default);
}
