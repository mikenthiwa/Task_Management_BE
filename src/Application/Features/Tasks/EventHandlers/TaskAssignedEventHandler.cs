using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enum;
using Domain.Events;
using MediatR;

namespace Application.Features.Tasks.EventHandlers;

public class TaskAssignedEventHandler(
    INotificationDispatcher notificationDispatcher
    ) : INotificationHandler<TaskAssignedEvent>
{
    public async Task Handle(TaskAssignedEvent notification, CancellationToken cancellationToken)
    {
        var integrationEvent = new NotificationIntegrationEvent
        {
            Type = NotificationType.TaskAssigned,
            Message = $"You have been assigned to task '{notification.Title}'.",
            UserId = notification.AssigneeId
        };
        await notificationDispatcher.DispatchAsync(integrationEvent, routingKey: "task.assigned", cancellationToken);
    }
}
