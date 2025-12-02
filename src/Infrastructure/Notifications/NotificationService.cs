using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enum;

namespace Infrastructure.Notifications;

public class NotificationService(IApplicationDbContext applicationDbContext): INotificationService
{
    public async Task<Guid> CreateNotificationAsync(string userId, string message, NotificationType type)
    {
        var notification = new Notification(userId, type, message);
        applicationDbContext.Notifications.Add(notification);
        await applicationDbContext.SaveChangesAsync(CancellationToken.None);
        return notification.Id;
    }
    
    public IQueryable<Notification> GetUserNotificationsAsync(string userId)
    {
        return applicationDbContext.Notifications
            .OrderByDescending(t => t.CreatedAt)
            .Where(n => n.UserId == userId && !n.IsDeleted);
    }
    
    public async System.Threading.Tasks.Task MarkAllNotificationsAsReadAsync(string userId)
    {
        var notifications = applicationDbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted);
        
        foreach (var notification in notifications)
        {
            notification.GetType().GetProperty("IsRead")?.SetValue(notification, true);
            notification.GetType().GetProperty("ReadAt")?.SetValue(notification, DateTimeOffset.UtcNow);
        }
        
        await applicationDbContext.SaveChangesAsync(CancellationToken.None);
    }
}
