using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.AccessControl.Application.Roles;

namespace ECM.AccessControl.Application.Roles.Queries;

public sealed class GetRolesQueryHandler(IRoleRepository repository)
{
    private readonly IRoleRepository _repository = repository;

    public async Task<IReadOnlyCollection<RoleSummary>> HandleAsync(GetRolesQuery query, CancellationToken cancellationToken = default)
    {
        var roles = await _repository.GetAllAsync(cancellationToken);
        return [.. roles.Select(RoleSummaryMapper.FromRole)];
    }
}
