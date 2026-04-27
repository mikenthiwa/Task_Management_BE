using Application.Common.Models;

namespace Application.Common.Interfaces;

public interface INotificationDispatcher
{
    Task DispatchAsync(
        NotificationIntegrationEvent notification,
        string routingKey,
        CancellationToken cancellationToken = default);
}
