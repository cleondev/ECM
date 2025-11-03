using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.Abstractions.Users;

namespace ECM.IAM.Application.Users;

public sealed class UserLookupService(IUserRepository userRepository) : IUserLookupService
{
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<Guid?> FindUserIdByUpnAsync(string upn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(upn))
        {
            return null;
        }

        var normalizedUpn = upn.Trim();
        var user = await _userRepository.GetByEmailAsync(normalizedUpn, cancellationToken);
        return user?.Id;
    }

    public async Task<Guid?> FindPrimaryGroupIdByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user?.PrimaryGroupId;
    }
}
