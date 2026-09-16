using Domain.Common;
using Domain.Enum;
using Domain.ValueObjects;

namespace Domain.Entities;

public class Notification(string userId, NotificationType type, string message, Guid? sourceMessageId = null) : BaseAuditableEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string UserId { get; private set; } = userId;
    public DomainUser? User { get; private set; }
    public NotificationType Type { get; private set; } = type;
    public string Message { get; private set; } = message;
    public NotificationAction? Action { get; set; }
    public bool IsRead { get; private set; } = false;
    public DateTimeOffset? ReadAt { get; private set; }
    public bool IsDeleted { get; private set; } = false;
    public Guid? SourceMessageId { get; private set; } = sourceMessageId;
}
