namespace ECM.IAM.Application.Users.Queries;

using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.IAM.Application.Users;

public sealed class GetUserByEmailQueryHandler(IUserRepository repository)
{
    private readonly IUserRepository _repository = repository;

    public async Task<UserSummaryResult?> HandleAsync(GetUserByEmailQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Email))
        {
            return null;
        }

        var user = await _repository.GetByEmailAsync(query.Email, cancellationToken);
        return user?.ToResult();
    }
}
