using System.Threading;
using System.Threading.Tasks;
using ECM.AccessControl.Application.Roles;

namespace ECM.AccessControl.Application.Roles.Commands;

public sealed class DeleteRoleCommandHandler(IRoleRepository repository)
{
    private readonly IRoleRepository _repository = repository;

    public async Task<bool> HandleAsync(DeleteRoleCommand command, CancellationToken cancellationToken = default)
    {
        var role = await _repository.GetByIdAsync(command.RoleId, cancellationToken);
        if (role is null)
        {
            return false;
        }

        await _repository.DeleteAsync(role, cancellationToken);
        return true;
    }
}
