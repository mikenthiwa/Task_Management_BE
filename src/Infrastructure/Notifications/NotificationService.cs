using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enum;
using Domain.ValueObjects;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Notifications;

public class NotificationService(ApplicationDbContext applicationDbContext,
    INotificationPublisherService notificationPublisherService
    ): INotificationService
{
    public async Task<NotificationCreationResult> CreateNotificationAsync(
        Guid sourceMessageId,
        string userId,
        string message,
        NotificationType type,
        string? actionUrl,
        string? actionLabel,
        CancellationToken cancellationToken = default)
    {
        var existingNotification = await applicationDbContext.Notifications
            .SingleOrDefaultAsync(notification => notification.SourceMessageId == sourceMessageId, cancellationToken);
        if (existingNotification is not null)
        {
            await notificationPublisherService.PublishToUserAsync(existingNotification.UserId, existingNotification);
            return new NotificationCreationResult(existingNotification.Id, false);
        }

        var notification = new Notification(userId, type, message, sourceMessageId);

        if (!string.IsNullOrWhiteSpace(actionUrl)
            && !string.IsNullOrWhiteSpace(actionLabel))
        {
            notification.Action = new NotificationAction
            {
                ActionUrl = actionUrl,
                ActionLabel = actionLabel
            };
        }

        applicationDbContext.Notifications.Add(notification);
        try
        {
            await applicationDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_Notifications_SourceMessageId"
            })
        {
            applicationDbContext.Entry(notification).State = EntityState.Detached;
            existingNotification = await applicationDbContext.Notifications
                .SingleAsync(item => item.SourceMessageId == sourceMessageId, cancellationToken);
            await notificationPublisherService.PublishToUserAsync(existingNotification.UserId, existingNotification);
            return new NotificationCreationResult(existingNotification.Id, false);
        }

        await notificationPublisherService.PublishToUserAsync(userId, notification);
        return new NotificationCreationResult(notification.Id, true);
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
