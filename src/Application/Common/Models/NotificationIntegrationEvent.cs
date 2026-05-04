using Domain.Enum;

namespace Application.Common.Models;

public sealed record NotificationIntegrationEvent
{
    public NotificationType Type { get; init; }
    public string Message { get; init; } = default!;
    public string UserId { get; init; } = default!;
    public string? ActionUrl { get; init; }
    public string? ActionLabel { get; init; }
    public DateTimeOffset OccuredAt { get; init; } = DateTimeOffset.UtcNow;
}
