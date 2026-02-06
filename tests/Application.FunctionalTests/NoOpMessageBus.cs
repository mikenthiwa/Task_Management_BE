using IMessageBus = Application.Common.Interfaces.IMessageBus;

namespace Application.FunctionalTests;

public class NoOpMessageBus : IMessageBus
{
    public Task PublishAsync<T>(
        T message,
        string exchange,
        string routingKey,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
