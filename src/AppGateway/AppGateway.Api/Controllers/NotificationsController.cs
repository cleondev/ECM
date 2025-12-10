using AppGateway.Api.Auth;
using AppGateway.Api.Notifications;
using AppGateway.Contracts.Notifications;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace AppGateway.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(AuthenticationSchemes = GatewayAuthenticationSchemes.Default)]
public sealed class NotificationsController(INotificationProvider notificationProvider) : ControllerBase
{
    private readonly INotificationProvider _notificationProvider = notificationProvider;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<NotificationItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var userObjectId = User.GetObjectId();
        var notifications = await _notificationProvider.GetNotificationsAsync(userObjectId, cancellationToken);
        return Ok(notifications);
    }
}
