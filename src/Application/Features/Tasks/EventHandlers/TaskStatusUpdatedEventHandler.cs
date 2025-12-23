using Application.Common.Interfaces;
using Domain.Enum;
using Domain.Events;
using MediatR;

namespace Application.Features.Tasks.EventHandlers;

public class TaskStatusUpdatedEventHandler(INotificationService notificationService, IApplicationDbContext applicationDb) : INotificationHandler<TaskStatusUpdatedEvent>
{
    public async Task Handle(TaskStatusUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var task = await applicationDb.Tasks.FindAsync(new object[] { notification.TaskId }, cancellationToken);

        if (task == null)
            return;

        var message = $"Task '{notification.Title}' status has been updated from {notification.OldStatus} to {notification.NewStatus}.";

        // Notify the task creator if they're not the one who updated it
        if (task.CreatorId != null && task.CreatorId != notification.UpdatedBy)
        {
            await notificationService.CreateNotificationAsync(task.CreatorId, message, NotificationType.TaskStatusUpdated);
        }
    }
}
