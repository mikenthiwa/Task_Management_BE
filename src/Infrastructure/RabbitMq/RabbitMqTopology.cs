using RabbitMQ.Client;

namespace Infrastructure.RabbitMq;

public static class RabbitMqTopology
{
    public const string TaskEventsExchange = "task.events";
    public const string NotificationQueue = "notification.task.events";
    public const string TaskEventsRoutingPattern = "task.*";
    public const string ReportEventsRoutingPattern = "report.*";
    public const string DeadLetterExchange = "notification.dead-letter";
    public const string DeadLetterQueue = "notification.task.events.dead-letter";
    public const string DeadLetterRoutingKey = "notification.task.events";

    public static async Task DeclareAsync(IChannel channel, CancellationToken cancellationToken = default)
    {
        await channel.ExchangeDeclareAsync(
            TaskEventsExchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(
            NotificationQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            NotificationQueue,
            TaskEventsExchange,
            TaskEventsRoutingPattern,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            NotificationQueue,
            TaskEventsExchange,
            ReportEventsRoutingPattern,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            DeadLetterExchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(
            DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            DeadLetterQueue,
            DeadLetterExchange,
            DeadLetterRoutingKey,
            cancellationToken: cancellationToken);
    }
}
