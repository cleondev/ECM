namespace ECM.IAM.Application.Groups;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.IAM.Domain.Users;

public interface IGroupService
{
    Task EnsureUserGroupsAsync(
        User user,
        IReadOnlyCollection<GroupAssignment> assignments,
        Guid? primaryGroupId,
        CancellationToken cancellationToken);
}
