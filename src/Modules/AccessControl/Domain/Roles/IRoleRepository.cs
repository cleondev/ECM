namespace ECM.Modules.AccessControl.Domain.Roles;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IRoleRepository
{
    Task<IReadOnlyCollection<Role>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task AddAsync(Role role, CancellationToken cancellationToken = default);

    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);

    Task DeleteAsync(Role role, CancellationToken cancellationToken = default);
}
