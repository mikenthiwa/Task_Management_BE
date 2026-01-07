using Microsoft.EntityFrameworkCore;

namespace Domain.ValueObjects;

[Owned]
public class NotificationAction
{
    public string ActionUrl { get; init; } = null!;
    public string ActionLabel { get; init; } = null!;
}
