using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.AccessControl.Application.Roles;
using ECM.AccessControl.Domain.Roles;
using ECM.AccessControl.Domain.Users;

namespace ECM.AccessControl.Application.Users;

public sealed class UserApplicationService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ISystemClock clock)
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRoleRepository _roleRepository = roleRepository;
    private readonly ISystemClock _clock = clock;

    public async Task<IReadOnlyCollection<UserSummary>> GetAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Select(MapToSummary).ToArray();
    }

    public async Task<UserSummary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        return user is null ? null : MapToSummary(user);
    }

    public async Task<OperationResult<UserSummary>> CreateAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        if (await _userRepository.GetByEmailAsync(command.Email, cancellationToken) is not null)
        {
            return OperationResult<UserSummary>.Failure($"A user with email '{command.Email}' already exists.");
        }

        User user;
        try
        {
            user = User.Create(
                command.Email,
                command.DisplayName,
                _clock.UtcNow,
                command.Department,
                command.IsActive);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<UserSummary>.Failure(exception.Message);
        }

        if (command.RoleIds.Count > 0)
        {
            foreach (var roleId in command.RoleIds.Distinct())
            {
                var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
                if (role is null)
                {
                    return OperationResult<UserSummary>.Failure($"Role '{roleId}' was not found.");
                }

                user.AssignRole(role);
            }
        }

        await _userRepository.AddAsync(user, cancellationToken);

        return OperationResult<UserSummary>.Success(MapToSummary(user));
    }

    public async Task<OperationResult<UserSummary>> UpdateAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return OperationResult<UserSummary>.Failure($"User '{command.UserId}' was not found.");
        }

        try
        {
            user.UpdateDisplayName(command.DisplayName);
            user.UpdateDepartment(command.Department);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<UserSummary>.Failure(exception.Message);
        }

        if (command.IsActive.HasValue)
        {
            if (command.IsActive.Value)
            {
                user.Activate();
            }
            else
            {
                user.Deactivate();
            }
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        return OperationResult<UserSummary>.Success(MapToSummary(user));
    }

    public async Task<OperationResult<UserSummary>> AssignRoleAsync(AssignUserRoleCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return OperationResult<UserSummary>.Failure($"User '{command.UserId}' was not found.");
        }

        var role = await _roleRepository.GetByIdAsync(command.RoleId, cancellationToken);
        if (role is null)
        {
            return OperationResult<UserSummary>.Failure($"Role '{command.RoleId}' was not found.");
        }

        user.AssignRole(role);
        await _userRepository.UpdateAsync(user, cancellationToken);

        return OperationResult<UserSummary>.Success(MapToSummary(user));
    }

    public async Task<OperationResult<UserSummary>> RemoveRoleAsync(RemoveUserRoleCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return OperationResult<UserSummary>.Failure($"User '{command.UserId}' was not found.");
        }

        user.RemoveRole(command.RoleId);
        await _userRepository.UpdateAsync(user, cancellationToken);

        return OperationResult<UserSummary>.Success(MapToSummary(user));
    }

    private static UserSummary MapToSummary(User user)
    {
        return new UserSummary(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Department,
            user.IsActive,
            user.CreatedAtUtc,
            user.Roles.Select(link => new RoleSummary(link.RoleId, link.Role?.Name ?? string.Empty)).ToArray());
    }
}
