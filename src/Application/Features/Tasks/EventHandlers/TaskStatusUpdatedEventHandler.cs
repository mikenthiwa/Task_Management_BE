using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enum;
using Domain.Events;
using MediatR;

namespace Application.Features.Tasks.EventHandlers;

public class TaskStatusUpdatedEventHandler(
    IApplicationDbContext applicationDb,
    IMessageBus messageBus
    ) : INotificationHandler<TaskStatusUpdatedEvent>
{
    public async Task Handle(TaskStatusUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var task = await applicationDb.Tasks.FindAsync([notification.TaskId], cancellationToken);

        if (task == null) return;

        var message = $"Task '{notification.Title}' status has been updated from {notification.OldStatus} to {notification.NewStatus}.";
        if (task.CreatorId != null && task.CreatorId != notification.UpdatedBy)
        {
            var integrationEvent = new NotificationIntegrationEvent
            {
                Type = NotificationType.TaskStatusUpdated,
                Message = message,
                UserId = task.CreatorId
            };
            await messageBus.PublishAsync(integrationEvent, exchange: "task.events", routingKey: "task.updated", cancellationToken);
        }
    }
}
