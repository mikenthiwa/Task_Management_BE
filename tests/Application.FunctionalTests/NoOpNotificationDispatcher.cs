using Application.Common.Interfaces;
using Application.Common.Models;

namespace Application.FunctionalTests;

public class NoOpNotificationDispatcher : INotificationDispatcher
{
    public Task DispatchAsync(
        NotificationIntegrationEvent notification,
        string routingKey,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
