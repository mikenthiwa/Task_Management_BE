namespace Application.Features.Tasks.IntegrationEvent;

public sealed record TaskCreatedIntegrationEvent
{
    public int TaskId { get; init; }
    public string Title { get; init; } = default!;
    public string? Description { get; init; } = default;
    public string CreatorId { get; init; } = default!;
    public DateTimeOffset OccuredAt { get; init; } = DateTimeOffset.UtcNow;
}
