using System;
using System.Linq;
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

    public async Task<bool> UserHasAnyRoleAsync(
        Guid userId,
        string[] roleNames,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || roleNames is null || roleNames.Length == 0)
        {
            return false;
        }

        var normalizedRoleNames = roleNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToArray();

        if (normalizedRoleNames.Length == 0)
        {
            return false;
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        return user.Roles.Any(link =>
            link.Role is not null
            && normalizedRoleNames.Contains(link.Role.Name, StringComparer.OrdinalIgnoreCase));
    }
}
