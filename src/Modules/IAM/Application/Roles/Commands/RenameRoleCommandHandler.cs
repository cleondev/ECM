using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.IAM.Application.Roles;
using ECM.IAM.Domain.Roles;

namespace ECM.IAM.Application.Roles.Commands;

public sealed class RenameRoleCommandHandler(IRoleRepository repository)
{
    private readonly IRoleRepository _repository = repository;

    public async Task<OperationResult<RoleSummary>> HandleAsync(RenameRoleCommand command, CancellationToken cancellationToken = default)
    {
        var role = await _repository.GetByIdAsync(command.RoleId, cancellationToken);
        if (role is null)
        {
            return OperationResult<RoleSummary>.Failure($"Role '{command.RoleId}' was not found.");
        }

        var existing = await _repository.GetByNameAsync(command.Name, cancellationToken);
        if (existing is not null && existing.Id != role.Id)
        {
            return OperationResult<RoleSummary>.Failure($"Role '{command.Name}' already exists.");
        }

        try
        {
            role.Rename(command.Name);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<RoleSummary>.Failure(exception.Message);
        }

        await _repository.UpdateAsync(role, cancellationToken);

        return OperationResult<RoleSummary>.Success(RoleSummaryMapper.FromRole(role));
    }
}
