using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.IAM.Application.Roles;
using ECM.IAM.Application.Users;
using ECM.IAM.Domain.Users;

namespace ECM.IAM.Application.Users.Commands;

public sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ISystemClock clock,
    IPasswordHasher passwordHasher)
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRoleRepository _roleRepository = roleRepository;
    private readonly ISystemClock _clock = clock;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    public async Task<OperationResult<UserSummaryResult>> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        if (await _userRepository.GetByEmailAsync(command.Email, cancellationToken) is not null)
        {
            return OperationResult<UserSummaryResult>.Failure($"A user with email '{command.Email}' already exists.");
        }

        User user;
        string? passwordHash = null;

        if (!string.IsNullOrEmpty(command.Password))
        {
            try
            {
                passwordHash = _passwordHasher.HashPassword(command.Password);
            }
            catch (ArgumentException exception)
            {
                return OperationResult<UserSummaryResult>.Failure(exception.Message);
            }
        }

        try
        {
            user = User.Create(
                command.Email,
                command.DisplayName,
                _clock.UtcNow,
                command.Department,
                command.IsActive,
                passwordHash);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<UserSummaryResult>.Failure(exception.Message);
        }

        if (command.RoleIds.Count > 0)
        {
            foreach (var roleId in command.RoleIds.Distinct())
            {
                var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
                if (role is null)
                {
                    return OperationResult<UserSummaryResult>.Failure($"Role '{roleId}' was not found.");
                }

                user.AssignRole(role, _clock.UtcNow);
            }
        }

        await _userRepository.AddAsync(user, cancellationToken);

        return OperationResult<UserSummaryResult>.Success(user.ToResult());
    }
}
