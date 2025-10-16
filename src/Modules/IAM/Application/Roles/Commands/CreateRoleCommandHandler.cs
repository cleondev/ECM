using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.IAM.Application.Roles;
using ECM.IAM.Domain.Roles;

namespace ECM.IAM.Application.Roles.Commands;

public sealed class CreateRoleCommandHandler(IRoleRepository repository)
{
    private readonly IRoleRepository _repository = repository;

    public async Task<OperationResult<RoleSummaryResult>> HandleAsync(CreateRoleCommand command, CancellationToken cancellationToken = default)
    {
        if (await _repository.GetByNameAsync(command.Name, cancellationToken) is not null)
        {
            return OperationResult<RoleSummaryResult>.Failure($"Role '{command.Name}' already exists.");
        }

        Role role;
        try
        {
            role = Role.Create(command.Name);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<RoleSummaryResult>.Failure(exception.Message);
        }

        await _repository.AddAsync(role, cancellationToken);

        return OperationResult<RoleSummaryResult>.Success(role.ToResult());
    }
}
