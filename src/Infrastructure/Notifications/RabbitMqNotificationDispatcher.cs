using Application.Common.Interfaces;
using Application.Common.Models;

namespace Infrastructure.Notifications;

public sealed class RabbitMqNotificationDispatcher(IMessageBus messageBus) : INotificationDispatcher
{
    public Task DispatchAsync(
        NotificationIntegrationEvent notification,
        string routingKey,
        CancellationToken cancellationToken = default)
    {
        return messageBus.PublishAsync(
            notification,
            exchange: "task.events",
            routingKey: routingKey,
            cancellationToken);
    }
}
