namespace Application.Common.Interfaces;

public interface IMessageBus
{
    Task PublishAsync<T>(
        T message,
        string exchange,
        string routingKey,
        CancellationToken cancellationToken = default
        );
}
