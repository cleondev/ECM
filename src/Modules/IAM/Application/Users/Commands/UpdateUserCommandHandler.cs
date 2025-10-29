using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.IAM.Application.Groups;
using ECM.IAM.Application.Users;

namespace ECM.IAM.Application.Users.Commands;

public sealed class UpdateUserCommandHandler(IUserRepository userRepository, IGroupService groupService)
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IGroupService _groupService = groupService;

    public async Task<OperationResult<UserSummaryResult>> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return OperationResult<UserSummaryResult>.Failure($"User '{command.UserId}' was not found.");
        }

        try
        {
            user.UpdateDisplayName(command.DisplayName);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<UserSummaryResult>.Failure(exception.Message);
        }

        if (command.IsActive.HasValue)
        {
            if (command.IsActive.Value)
            {
                user.Activate();
            }
            else
            {
                user.Deactivate();
            }
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        var groups = BuildGroupAssignments(command.Groups);
        await _groupService.EnsureUserGroupsAsync(user, groups, cancellationToken);

        return OperationResult<UserSummaryResult>.Success(user.ToResult());
    }

    private static IReadOnlyCollection<GroupAssignment> BuildGroupAssignments(IReadOnlyCollection<GroupAssignment> requested)
    {
        var assignments = new List<GroupAssignment>
        {
            GroupAssignment.System(),
            GroupAssignment.Guest(),
        };

        if (requested is { Count: > 0 })
        {
            assignments.AddRange(requested.Select(assignment => assignment.Normalize()));
        }

        return assignments;
    }
}
