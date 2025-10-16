using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.IAM.Application.Users;
using ECM.BuildingBlocks.Application;

namespace ECM.IAM.Application.Users.Commands;

public sealed class UpdateUserProfileCommandHandler(IUserRepository userRepository)
{
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<OperationResult<UserSummary>> HandleAsync(
        UpdateUserProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return OperationResult<UserSummary>.Failure("User email is required.");
        }

        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null)
        {
            return OperationResult<UserSummary>.Failure($"User with email '{command.Email}' was not found.");
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

        await _userRepository.UpdateAsync(user, cancellationToken);

        return OperationResult<UserSummary>.Success(UserSummaryMapper.ToSummary(user));
    }
}
