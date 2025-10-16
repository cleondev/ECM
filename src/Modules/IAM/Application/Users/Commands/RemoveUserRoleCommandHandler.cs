using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.IAM.Application.Users;

namespace ECM.IAM.Application.Users.Commands;

public sealed class RemoveUserRoleCommandHandler(
    IUserRepository userRepository,
    ISystemClock clock)
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<UserSummary>> HandleAsync(RemoveUserRoleCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return OperationResult<UserSummary>.Failure($"User '{command.UserId}' was not found.");
        }

        user.RemoveRole(command.RoleId, _clock.UtcNow);
        await _userRepository.UpdateAsync(user, cancellationToken);

        return OperationResult<UserSummary>.Success(UserSummaryMapper.ToSummary(user));
    }
}
