using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.IAM.Application.Groups;
using ECM.IAM.Application.Users;
using ECM.BuildingBlocks.Application;

namespace ECM.IAM.Application.Users.Commands;

public sealed class UpdateUserProfileCommandHandler(IUserRepository userRepository, IGroupService groupService)
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IGroupService _groupService = groupService;

    public async Task<OperationResult<UserSummaryResult>> HandleAsync(
        UpdateUserProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return OperationResult<UserSummaryResult>.Failure("User email is required.");
        }

        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null)
        {
            return OperationResult<UserSummaryResult>.Failure($"User with email '{command.Email}' was not found.");
        }

        try
        {
            user.UpdateDisplayName(command.DisplayName);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<UserSummaryResult>.Failure(exception.Message);
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        var assignments = BuildAssignments(command.GroupIds, command.PrimaryGroupId);
        await _groupService.EnsureUserGroupsAsync(user, assignments, command.PrimaryGroupId, cancellationToken);

        return OperationResult<UserSummaryResult>.Success(user.ToResult());
    }

    private static IReadOnlyCollection<GroupAssignment> BuildAssignments(
        IReadOnlyCollection<Guid> groupIds,
        Guid? primaryGroupId)
    {
        var assignments = new List<GroupAssignment>();
        var seen = new HashSet<Guid>();

        if (groupIds is { Count: > 0 })
        {
            foreach (var groupId in groupIds)
            {
                if (groupId == Guid.Empty)
                {
                    continue;
                }

                if (seen.Add(groupId))
                {
                    assignments.Add(GroupAssignment.ForExistingGroup(groupId));
                }
            }
        }

        if (primaryGroupId.HasValue
            && primaryGroupId.Value != Guid.Empty
            && seen.Add(primaryGroupId.Value))
        {
            assignments.Add(GroupAssignment.ForExistingGroup(primaryGroupId.Value));
        }

        return assignments;
    }
}
