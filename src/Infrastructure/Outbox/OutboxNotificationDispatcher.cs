using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Models;
using Infrastructure.Data;
using Infrastructure.RabbitMq;

namespace Infrastructure.Outbox;

public sealed class OutboxNotificationDispatcher(
    ApplicationDbContext dbContext,
    TimeProvider timeProvider) : INotificationDispatcher
{
    public Task DispatchAsync(
        NotificationIntegrationEvent notification,
        string routingKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var message = new OutboxMessage(
            typeof(NotificationIntegrationEvent).FullName!,
            JsonSerializer.Serialize(notification),
            RabbitMqTopology.TaskEventsExchange,
            routingKey,
            timeProvider.GetUtcNow());

        dbContext.OutboxMessages.Add(message);
        return Task.CompletedTask;
    }
}
