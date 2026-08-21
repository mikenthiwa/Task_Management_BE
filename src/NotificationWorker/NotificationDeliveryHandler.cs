using Application.Common.Interfaces;
using Application.Common.Models;
using Infrastructure.RabbitMq;

namespace NotificationWorker;

public sealed class NotificationDeliveryHandler(
    INotificationDeliveryProcessor processor,
    IMessageBus messageBus,
    TimeProvider timeProvider,
    ILogger<NotificationDeliveryHandler> logger)
{
    public async Task HandleAsync(
        string? rawMessageId,
        byte[] payload,
        Func<CancellationToken, ValueTask> acknowledge,
        Func<CancellationToken, ValueTask> requeue,
        CancellationToken cancellationToken)
    {
        var hasValidMessageId = Guid.TryParse(rawMessageId, out var messageId);
        var result = hasValidMessageId
            ? await processor.ProcessAsync(messageId, payload, cancellationToken)
            : NotificationProcessingResult.Failure("The RabbitMQ message ID is missing or invalid.");

        if (result.Succeeded)
        {
            await acknowledge(cancellationToken);
            return;
        }

        messageId = hasValidMessageId ? messageId : Guid.NewGuid();
        try
        {
            var deadLetter = new DeadLetterNotification(
                messageId,
                Convert.ToBase64String(payload),
                result.Error ?? "Notification processing failed.",
                timeProvider.GetUtcNow());
            await messageBus.PublishAsync(
                deadLetter,
                RabbitMqTopology.DeadLetterExchange,
                RabbitMqTopology.DeadLetterRoutingKey,
                messageId,
                cancellationToken);
            await acknowledge(cancellationToken);
            logger.LogError(
                "RabbitMQ message {MessageId} was moved to the dead-letter queue: {Error}",
                messageId,
                result.Error);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Dead-lettering RabbitMQ message {MessageId} failed; requeueing it.", messageId);
            await requeue(cancellationToken);
        }
    }
}
