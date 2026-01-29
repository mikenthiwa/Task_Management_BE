using System.Text;
using System.Text.Json;
using Application.Common.Interfaces;
using RabbitMQ.Client;

namespace Infrastructure.RabbitMq;

public sealed class RabbitMqMessageBus : IMessageBus, IDisposable
{
    private readonly Task<IChannel> _channel;
    
    public RabbitMqMessageBus(string hostName, string userName, string password) {
        var channelOpt = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
            );
        var factory = new ConnectionFactory { HostName = hostName, UserName = userName, Password = password };
        var connection = factory.CreateConnectionAsync();
        _channel = connection.Result.CreateChannelAsync(channelOpt);
        _channel.Result.ExchangeDeclareAsync(exchange: "task.events", type: ExchangeType.Topic, durable: true);
    }

    public async Task PublishAsync<T>(T message, string exchange, string routingKey, CancellationToken cancellationToken = default)
    {
        var props = new BasicProperties { Persistent = true };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        await _channel.Result.BasicPublishAsync(exchange: exchange, routingKey: routingKey, body: body, basicProperties: props, mandatory: true, cancellationToken: cancellationToken);
    }
    
    public void Dispose()
    {
        _channel.Result.CloseAsync();
        _channel.Result.Dispose();
    }
}
