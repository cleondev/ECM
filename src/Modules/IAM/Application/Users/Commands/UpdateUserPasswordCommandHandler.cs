using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.IAM.Application.Users;

namespace ECM.IAM.Application.Users.Commands;

public sealed class UpdateUserPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher)
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    public async Task<OperationResult> HandleAsync(
        UpdateUserPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return OperationResult.Failure("User email is required.");
        }

        if (string.IsNullOrWhiteSpace(command.NewPassword))
        {
            return OperationResult.Failure("New password is required.");
        }

        var normalizedNewPassword = command.NewPassword.Trim();
        if (normalizedNewPassword.Length < 8)
        {
            return OperationResult.Failure("New password must be at least 8 characters long.");
        }

        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null)
        {
            return OperationResult.Failure($"User with email '{command.Email}' was not found.");
        }

        var hasExistingPassword = !string.IsNullOrWhiteSpace(user.PasswordHash);
        if (hasExistingPassword)
        {
            if (string.IsNullOrEmpty(command.CurrentPassword))
            {
                return OperationResult.Failure("Current password is required.");
            }

            if (!_passwordHasher.VerifyHashedPassword(user.PasswordHash!, command.CurrentPassword))
            {
                return OperationResult.Failure("Current password is incorrect.");
            }
        }

        if (hasExistingPassword && string.Equals(command.CurrentPassword, normalizedNewPassword, StringComparison.Ordinal))
        {
            return OperationResult.Failure("New password must be different from the current password.");
        }

        user.SetPasswordHash(_passwordHasher.HashPassword(normalizedNewPassword));
        await _userRepository.UpdateAsync(user, cancellationToken);

        return OperationResult.Success();
    }
}
