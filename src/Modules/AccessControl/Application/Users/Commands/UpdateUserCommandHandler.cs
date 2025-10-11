using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.AccessControl.Application.Users;

namespace ECM.AccessControl.Application.Users.Commands;

public sealed class UpdateUserCommandHandler(IUserRepository userRepository)
{
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<OperationResult<UserSummary>> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
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

        return OperationResult<UserSummary>.Success(UserSummaryMapper.ToSummary(user));
    }
}
