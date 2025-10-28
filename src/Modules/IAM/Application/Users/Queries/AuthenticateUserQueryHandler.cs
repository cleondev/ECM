using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.IAM.Application.Users;

namespace ECM.IAM.Application.Users.Queries;

public sealed class AuthenticateUserQueryHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher)
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    public async Task<OperationResult<UserSummaryResult>> HandleAsync(
        AuthenticateUserQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Email))
        {
            return OperationResult<UserSummaryResult>.Failure("Email is required.");
        }

        if (string.IsNullOrEmpty(query.Password))
        {
            return OperationResult<UserSummaryResult>.Failure("Password is required.");
        }

        var user = await _userRepository.GetByEmailAsync(query.Email.Trim(), cancellationToken);
        if (user is null || !user.IsActive)
        {
            return OperationResult<UserSummaryResult>.Failure("Invalid email or password.");
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return OperationResult<UserSummaryResult>.Failure("User does not have a local password configured.");
        }

        var isValid = _passwordHasher.VerifyHashedPassword(user.PasswordHash, query.Password);
        if (!isValid)
        {
            return OperationResult<UserSummaryResult>.Failure("Invalid email or password.");
        }

        return OperationResult<UserSummaryResult>.Success(user.ToResult());
    }
}
