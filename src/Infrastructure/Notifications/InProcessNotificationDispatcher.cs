using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.ValueObjects;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Notifications;

public sealed class InProcessNotificationDispatcher(INotificationService notificationService) : INotificationDispatcher
{
    public async Task DispatchAsync(
        NotificationIntegrationEvent notification,
        string routingKey,
        CancellationToken cancellationToken = default)
    {
        await notificationService.CreateNotificationAsync(notification.UserId, notification.Message, notification.Type, notification.ActionUrl, notification.ActionLabel);
    }
}
