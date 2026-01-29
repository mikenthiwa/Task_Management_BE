using Domain.Enum;

namespace Application.Common.Models;

public sealed record NotificationIntegrationEvent
{
    public NotificationType Type { get; init; }
    public string Message { get; init; } = default!;
    public string UserId { get; init; } = default!;
    public DateTimeOffset OccuredAt { get; init; } = DateTimeOffset.UtcNow;
}
