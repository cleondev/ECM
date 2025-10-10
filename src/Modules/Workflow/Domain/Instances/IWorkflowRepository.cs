using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ECM.Workflow.Domain.Instances;

public interface IWorkflowRepository
{
    Task AddAsync(WorkflowInstance instance, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorkflowInstance>> GetActiveAsync(CancellationToken cancellationToken = default);
}
