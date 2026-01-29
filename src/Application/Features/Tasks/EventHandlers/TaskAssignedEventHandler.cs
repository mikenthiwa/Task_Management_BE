using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enum;
using Domain.Events;
using MediatR;

namespace Application.Features.Tasks.EventHandlers;

public class TaskAssignedEventHandler(
    IMessageBus messageBus
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
        await messageBus.PublishAsync(integrationEvent, exchange: "task.events", routingKey: "task.assigned", cancellationToken);
    }
}
