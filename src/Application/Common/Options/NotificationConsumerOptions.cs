namespace Application.Common.Options;

public sealed class NotificationConsumerOptions
{
    public const string SectionName = "NotificationConsumer";

    public ushort PrefetchCount { get; init; } = 10;
    public int MaximumAttempts { get; init; } = 3;
    public int[] RetryDelaySeconds { get; init; } = [1, 5];
    public int ReconnectDelaySeconds { get; init; } = 5;
}
