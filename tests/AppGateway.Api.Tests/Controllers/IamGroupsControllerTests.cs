using AppGateway.Api.Controllers.IAM;
using AppGateway.Contracts.IAM.Groups;
using AppGateway.Infrastructure.Ecm;

using FluentAssertions;

using Xunit;

namespace AppGateway.Api.Tests.Controllers;

public class IamGroupsControllerTests
{
    [Fact]
    public async Task GetAsync_ReturnsGroups_FromClient()
    {
        var groups = new[]
        {
            new GroupSummaryDto(Guid.NewGuid(), "Group A", "role", "GroupMember"),
            new GroupSummaryDto(Guid.NewGuid(), "Group B", "role", "GroupMember")
        };

        var client = new TrackingEcmApiClient(groups);
        var controller = new IamGroupsController(client);

        var result = await controller.GetAsync(CancellationToken.None);

        result.Should().BeEquivalentTo(groups);
        client.GetGroupsCalls.Should().Be(1);
    }

    private sealed class TrackingEcmApiClient(IReadOnlyCollection<GroupSummaryDto> groups) : IGroupsApiClient
    {
        private readonly IReadOnlyCollection<GroupSummaryDto> groups = groups;

        public int GetGroupsCalls { get; private set; }

        public Task<IReadOnlyCollection<GroupSummaryDto>> GetGroupsAsync(CancellationToken cancellationToken = default)
        {
            GetGroupsCalls++;
            return Task.FromResult(groups);
        }

        public Task<GroupSummaryDto?> CreateGroupAsync(CreateGroupRequestDto requestDto, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GroupSummaryDto?> UpdateGroupAsync(Guid groupId, UpdateGroupRequestDto requestDto, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
