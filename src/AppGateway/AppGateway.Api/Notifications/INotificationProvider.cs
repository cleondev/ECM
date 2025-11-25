using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Contracts.Notifications;

namespace AppGateway.Api.Notifications;

public interface INotificationProvider
{
    Task<IReadOnlyCollection<NotificationItemDto>> GetNotificationsAsync(
        string? userObjectId,
        CancellationToken cancellationToken = default);
}
