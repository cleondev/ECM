namespace ECM.AccessControl.Application.Relations;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.AccessControl.Domain.Relations;

public interface IAccessRelationRepository
{
    Task<IReadOnlyCollection<AccessRelation>> GetBySubjectAsync(Guid subjectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccessRelation>> GetByObjectAsync(string objectType, Guid objectId, CancellationToken cancellationToken = default);

    Task<AccessRelation?> GetAsync(Guid subjectId, string objectType, Guid objectId, string relation, CancellationToken cancellationToken = default);

    Task AddAsync(AccessRelation relation, CancellationToken cancellationToken = default);

    Task DeleteAsync(AccessRelation relation, CancellationToken cancellationToken = default);
}
