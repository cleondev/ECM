using System;
using System.Threading;
using System.Threading.Tasks;

namespace ECM.Abstractions.Users;

public interface IUserLookupService
{
    Task<Guid?> FindUserIdByUpnAsync(string upn, CancellationToken cancellationToken = default);

    Task<Guid?> FindPrimaryGroupIdByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> UserHasAnyRoleAsync(
        Guid userId,
        string[] roleNames,
        CancellationToken cancellationToken = default);
}
