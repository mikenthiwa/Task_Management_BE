using Application.Common.Interfaces;
using Application.Features.Tasks.Queries.GetTasksWithPagination;
using Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace Application.FunctionalTests;

public sealed class RecordingNotificationPublisherService : INotificationPublisherService
{
    private readonly object _sync = new();
    private readonly List<Guid> _publishedNotificationIds = [];

    public IReadOnlyList<Guid> PublishedNotificationIds
    {
        get
        {
            lock (_sync)
            {
                return _publishedNotificationIds.ToList();
            }
        }
    }

    public Task PublishAsync(Notification notification)
    {
        Record(notification.Id);
        return Task.CompletedTask;
    }

    public Task PublishToUserAsync(string userId, Notification notification)
    {
        Record(notification.Id);
        return Task.CompletedTask;
    }

    public Task NotifyTaskUpdatedAsync(TaskDto taskDto)
    {
        return Task.CompletedTask;
    }

    public void Clear()
    {
        lock (_sync)
        {
            _publishedNotificationIds.Clear();
        }
    }

    private void Record(Guid notificationId)
    {
        lock (_sync)
        {
            _publishedNotificationIds.Add(notificationId);
        }
    }
}
