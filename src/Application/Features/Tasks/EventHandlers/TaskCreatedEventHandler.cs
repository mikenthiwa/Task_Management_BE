using Application.Common.Interfaces;
using Application.Features.Tasks.IntegrationEvent;
using Domain.Enum;
using Domain.Events;
using MediatR;

namespace Application.Features.Tasks.EventHandlers;

public class TaskCreatedEventHandler(
    // INotificationService notificationService, 
    IMessageBus messageBus
    ) : INotificationHandler<TaskCreatedEvent>
{
    public async Task Handle(TaskCreatedEvent notification, CancellationToken cancellationToken)
    {
        var task = notification.TaskItem;
        // var message = $"Task '{task.Title}' has been created.";
        // await notificationService.CreateNotificationAsync(task.CreatorId!, message, NotificationType.TaskCreated, null, null);
        var integrationEvent = new TaskCreatedIntegrationEvent()
        {
            TaskId = task.Id,
            Title = task.Title,
            Description = task.Description,
            CreatorId = task.CreatorId!,
        };
        await messageBus.PublishAsync(integrationEvent, exchange: "task.events", routingKey: "task.created", cancellationToken);
    }
}
