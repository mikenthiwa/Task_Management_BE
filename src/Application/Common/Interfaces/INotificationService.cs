using Domain.Entities;
using Domain.Enum;
using Application.Common.Models;
using Task = System.Threading.Tasks.Task;

namespace Application.Common.Interfaces;

public interface INotificationService
{
    Task<NotificationCreationResult> CreateNotificationAsync(
        Guid sourceMessageId,
        string userId,
        string message,
        NotificationType type,
        string? actionUrl,
        string? actionLabel,
        CancellationToken cancellationToken = default);
    IQueryable<Notification> GetUserNotificationsAsync(string userId);
    Task MarkAllNotificationsAsReadAsync(string userId);
}
