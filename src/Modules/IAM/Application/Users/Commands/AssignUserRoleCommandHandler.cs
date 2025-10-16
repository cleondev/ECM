using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.IAM.Application.Roles;
using ECM.IAM.Application.Users;

namespace ECM.IAM.Application.Users.Commands;

public sealed class AssignUserRoleCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ISystemClock clock)
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRoleRepository _roleRepository = roleRepository;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<UserSummary>> HandleAsync(AssignUserRoleCommand command, CancellationToken cancellationToken = default)
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

        user.AssignRole(role, _clock.UtcNow);
        await _userRepository.UpdateAsync(user, cancellationToken);

        return OperationResult<UserSummary>.Success(UserSummaryMapper.ToSummary(user));
    }
}
