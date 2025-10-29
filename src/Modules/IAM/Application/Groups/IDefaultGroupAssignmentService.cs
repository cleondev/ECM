using System.Threading;
using System.Threading.Tasks;
using ECM.IAM.Domain.Users;

namespace ECM.IAM.Application.Groups;

public interface IDefaultGroupAssignmentService
{
    Task AssignAsync(User user, CancellationToken cancellationToken = default);
}
