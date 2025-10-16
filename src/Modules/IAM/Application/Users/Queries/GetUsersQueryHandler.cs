using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.IAM.Application.Users;

namespace ECM.IAM.Application.Users.Queries;

public sealed class GetUsersQueryHandler(IUserRepository userRepository)
{
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<IReadOnlyCollection<UserSummary>> HandleAsync(GetUsersQuery query, CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return [.. users.Select(UserSummaryMapper.ToSummary)];
    }
}
