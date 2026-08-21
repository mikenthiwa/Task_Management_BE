namespace Application.Common.Options;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int PollIntervalMilliseconds { get; init; } = 1_000;
    public int BatchSize { get; init; } = 50;
    public int LeaseSeconds { get; init; } = 60;
    public int InitialRetryDelaySeconds { get; init; } = 5;
    public int MaximumRetryDelaySeconds { get; init; } = 300;
    public int ProcessedRetentionDays { get; init; } = 7;
}
