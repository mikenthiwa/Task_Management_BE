using IMessageBus = Application.Common.Interfaces.IMessageBus;

namespace Application.FunctionalTests;

public class NoOpMessageBus : IMessageBus
{
    private readonly List<Guid> _publishedMessageIds = [];

    public IReadOnlyList<Guid> PublishedMessageIds => _publishedMessageIds;
    public bool ThrowOnPublish { get; set; }
    public Func<CancellationToken, Task>? BeforePublishAsync { get; set; }

    public async Task PublishAsync<T>(
        T message,
        string exchange,
        string routingKey,
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        if (BeforePublishAsync is not null)
        {
            await BeforePublishAsync(cancellationToken);
        }

        if (ThrowOnPublish)
        {
            throw new InvalidOperationException("Simulated publish failure.");
        }

        _publishedMessageIds.Add(messageId);
    }

    public void Reset()
    {
        ThrowOnPublish = false;
        BeforePublishAsync = null;
        _publishedMessageIds.Clear();
    }
}
