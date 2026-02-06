using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enum;
using Domain.Events;
using MediatR;

namespace Application.Features.Tasks.EventHandlers;

public class TaskCreatedEventHandler(
    IMessageBus messageBus
    ) : INotificationHandler<TaskCreatedEvent>
{
    public async Task Handle(TaskCreatedEvent notification, CancellationToken cancellationToken)
    {
        var task = notification.TaskItem;
        var integrationEvent = new NotificationIntegrationEvent
        {
            Type = NotificationType.TaskCreated,
            Message = $"Task '{task.Title}' has been created.",
            UserId = task.CreatorId!,
        };
        await messageBus.PublishAsync(integrationEvent, exchange: "task.events", routingKey: "task.created", cancellationToken);
    }
}
