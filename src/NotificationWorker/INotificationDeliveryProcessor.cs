namespace NotificationWorker;

public interface INotificationDeliveryProcessor
{
    Task<NotificationProcessingResult> ProcessAsync(
        Guid messageId,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken);
}
