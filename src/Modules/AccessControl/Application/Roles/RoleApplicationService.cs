using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.Modules.AccessControl.Domain.Roles;

namespace ECM.Modules.AccessControl.Application.Roles;

public sealed class RoleApplicationService(IRoleRepository repository)
{
    private readonly IRoleRepository _repository = repository;

    public async Task<IReadOnlyCollection<RoleSummary>> GetAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _repository.GetAllAsync(cancellationToken);
        return roles.Select(role => new RoleSummary(role.Id, role.Name)).ToArray();
    }

    public async Task<RoleSummary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _repository.GetByIdAsync(id, cancellationToken);
        return role is null ? null : new RoleSummary(role.Id, role.Name);
    }

    public async Task<OperationResult<RoleSummary>> CreateAsync(CreateRoleCommand command, CancellationToken cancellationToken = default)
    {
        if (await _repository.GetByNameAsync(command.Name, cancellationToken) is not null)
        {
            return OperationResult<RoleSummary>.Failure($"Role '{command.Name}' already exists.");
        }

        Role role;
        try
        {
            role = Role.Create(command.Name);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<RoleSummary>.Failure(exception.Message);
        }

        await _repository.AddAsync(role, cancellationToken);

        return OperationResult<RoleSummary>.Success(new RoleSummary(role.Id, role.Name));
    }

    public async Task<OperationResult<RoleSummary>> RenameAsync(RenameRoleCommand command, CancellationToken cancellationToken = default)
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

        return OperationResult<RoleSummary>.Success(new RoleSummary(role.Id, role.Name));
    }

    public async Task<bool> DeleteAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _repository.GetByIdAsync(roleId, cancellationToken);
        if (role is null)
        {
            return false;
        }

        await _repository.DeleteAsync(role, cancellationToken);
        return true;
    }
}
