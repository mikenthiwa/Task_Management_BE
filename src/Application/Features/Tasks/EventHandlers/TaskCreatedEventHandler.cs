using Application.Common.Interfaces;
using Application.Features.Tasks.IntegrationEvent;
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
        var integrationEvent = new TaskCreatedIntegrationEvent
        {
            TaskId = task.Id,
            Title = task.Title,
            Description = task.Description,
            CreatorId = task.CreatorId!,
        };
        await messageBus.PublishAsync(integrationEvent, exchange: "task.events", routingKey: "task.created", cancellationToken);
    }
}
