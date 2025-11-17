using System;
using System.Collections.Generic;
using AppGateway.Api.Auth;
using AppGateway.Api.Controllers.IAM;
using AppGateway.Contracts.IAM.Groups;
using AppGateway.Contracts.IAM.Roles;
using AppGateway.Contracts.IAM.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AppGateway.Api.Tests.Controllers;

public class IamGroupsControllerTests
{
    [Fact]
    public void Get_ReturnsGroups_FromCurrentUserProfile()
    {
        var groups = new[]
        {
            new GroupSummaryDto(Guid.NewGuid(), "Group A", "role", "GroupMember"),
            new GroupSummaryDto(Guid.NewGuid(), "Group B", "role", "GroupMember")
        };

        var profile = new UserSummaryDto(
            Guid.NewGuid(),
            "user@example.com",
            "Example User",
            true,
            false,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            Array.Empty<Guid>(),
            Array.Empty<RoleSummaryDto>(),
            groups);

        var httpContext = new DefaultHttpContext();
        CurrentUserProfileStore.Set(httpContext, profile);

        var controller = new IamGroupsController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var result = controller.Get();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGroups = okResult.Value.Should().BeAssignableTo<IReadOnlyCollection<GroupSummaryDto>>().Subject;
        returnedGroups.Should().BeEquivalentTo(groups);
    }
}
