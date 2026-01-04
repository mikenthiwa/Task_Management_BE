using Application.Features.Tasks.Command.Queries.GetTasksWithPagination;
using Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace Application.Common.Interfaces;

public interface INotificationPublisherService
{
    Task PublishAsync(Notification notification);
    Task PublishToUserAsync(string userId, Notification notification);
    Task NotifyTaskUpdatedAsync(TaskDto taskDto);
}
