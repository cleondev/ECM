using System.Threading;
using System.Threading.Tasks;
using ECM.IAM.Application.Users;

namespace ECM.IAM.Application.Users.Queries;

public sealed class GetUserByIdQueryHandler(IUserRepository userRepository)
{
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<UserSummary?> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
        return user is null ? null : UserSummaryMapper.ToSummary(user);
    }
}
