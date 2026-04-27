using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.ValueObjects;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Notifications;

public sealed class InProcessNotificationDispatcher(
    IApplicationDbContext applicationDbContext,
    INotificationPublisherService notificationPublisherService) : INotificationDispatcher
{
    public async Task DispatchAsync(
        NotificationIntegrationEvent notification,
        string routingKey,
        CancellationToken cancellationToken = default)
    {
        var entity = new Notification(notification.UserId, notification.Type, notification.Message);

        if (!string.IsNullOrWhiteSpace(notification.ActionUrl)
            && !string.IsNullOrWhiteSpace(notification.ActionLabel))
        {
            entity.Action = new NotificationAction
            {
                ActionUrl = notification.ActionUrl,
                ActionLabel = notification.ActionLabel
            };
        }

        applicationDbContext.Notifications.Add(entity);
        await notificationPublisherService.PublishToUserAsync(notification.UserId, entity);
    }
}
