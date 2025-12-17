using Application.Common.Interfaces;
using Domain.Enum;
using Domain.Events;
using MediatR;

namespace Application.Features.Tasks.EventHandlers;

public class TaskCreatedEventHandler(INotificationService notificationService) : INotificationHandler<TaskCreatedEvent>
{
    public async Task Handle(TaskCreatedEvent notification, CancellationToken cancellationToken)
    {
        var task = notification.TaskItem;
        var message = $"Task '{task.Title}' has been created.";
        await notificationService.CreateNotificationAsync(task.CreatorId!, message, NotificationType.TaskCreated);
    }
}
