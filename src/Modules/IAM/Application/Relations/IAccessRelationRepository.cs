namespace ECM.IAM.Application.Relations;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.IAM.Domain.Relations;

public interface IAccessRelationRepository
{
    Task<IReadOnlyCollection<AccessRelation>> GetBySubjectAsync(string subjectType, Guid subjectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccessRelation>> GetByObjectAsync(string objectType, Guid objectId, CancellationToken cancellationToken = default);

    Task<AccessRelation?> GetAsync(string subjectType, Guid subjectId, string objectType, Guid objectId, string relation, CancellationToken cancellationToken = default);

    Task AddAsync(AccessRelation relation, CancellationToken cancellationToken = default);

    Task DeleteAsync(AccessRelation relation, CancellationToken cancellationToken = default);
}
