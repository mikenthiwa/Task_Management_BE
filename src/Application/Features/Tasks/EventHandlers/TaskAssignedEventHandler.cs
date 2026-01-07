using Application.Common.Interfaces;
using Domain.Enum;
using Domain.Events;
using MediatR;

namespace Application.Features.Tasks.EventHandlers;

public class TaskAssignedEventHandler(INotificationService notificationService ): INotificationHandler<TaskAssignedEvent>
{
    public async Task Handle(TaskAssignedEvent notification, CancellationToken cancellationToken)
    {
        await notificationService.CreateNotificationAsync(notification.AssigneeId, $"You have been assigned to task '{notification.Title}'.", NotificationType.TaskAssigned, null, null);
    }
}
