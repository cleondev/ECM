using System.Threading;
using System.Threading.Tasks;
using ECM.AccessControl.Application.Roles;

namespace ECM.AccessControl.Application.Roles.Queries;

public sealed class GetRoleByIdQueryHandler(IRoleRepository repository)
{
    private readonly IRoleRepository _repository = repository;

    public async Task<RoleSummary?> HandleAsync(GetRoleByIdQuery query, CancellationToken cancellationToken = default)
    {
        var role = await _repository.GetByIdAsync(query.RoleId, cancellationToken);
        return role is null ? null : RoleSummaryMapper.FromRole(role);
    }
}
