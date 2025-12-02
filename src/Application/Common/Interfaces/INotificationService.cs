using Domain.Entities;
using Domain.Enum;
using Task = System.Threading.Tasks.Task;

namespace Application.Common.Interfaces;

public interface INotificationService
{
    Task<Guid> CreateNotificationAsync(string userId, string message, NotificationType type);
    IQueryable<Notification> GetUserNotificationsAsync(string userId);
    Task MarkAllNotificationsAsReadAsync(string userId);
}
