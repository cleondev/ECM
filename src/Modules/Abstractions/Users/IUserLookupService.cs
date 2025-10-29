using System;
using System.Threading;
using System.Threading.Tasks;

namespace ECM.Abstractions.Users;

public interface IUserLookupService
{
    Task<Guid?> FindUserIdByUpnAsync(string upn, CancellationToken cancellationToken = default);
}
