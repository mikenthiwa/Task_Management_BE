using Infrastructure.RabbitMq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationWorker;

public sealed class Worker(
    NotificationDeliveryHandler deliveryHandler,
    Microsoft.Extensions.Options.IOptions<Application.Common.Options.NotificationConsumerOptions> options,
    ILogger<Worker> logger,
    string hostName,
    string userName,
    string password,
    string virtualHost,
    int port,
    bool useSsl) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
            VirtualHost = virtualHost,
            Port = port,
            AutomaticRecoveryEnabled = false,
            TopologyRecoveryEnabled = false
        };
        if (useSsl)
        {
            factory.Ssl = new SslOption { Enabled = true, ServerName = hostName };
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync(cancellationToken: stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                await RabbitMqTopology.DeclareAsync(_channel, stoppingToken);
                await _channel.BasicQosAsync(0, options.Value.PrefetchCount, global: false, stoppingToken);

                var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _connection.ConnectionShutdownAsync += (_, _) =>
                {
                    disconnected.TrySetResult();
                    return Task.CompletedTask;
                };
                _channel.ChannelShutdownAsync += (_, _) =>
                {
                    disconnected.TrySetResult();
                    return Task.CompletedTask;
                };

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += HandleMessageAsync;
                await _channel.BasicConsumeAsync(
                    RabbitMqTopology.NotificationQueue,
                    autoAck: false,
                    consumer,
                    stoppingToken);

                logger.LogInformation("The notification consumer is connected to RabbitMQ.");
                await disconnected.Task.WaitAsync(stoppingToken);
                logger.LogWarning("The notification consumer lost its RabbitMQ connection.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "The notification consumer could not connect to RabbitMQ. Retrying in {DelaySeconds} seconds.",
                    options.Value.ReconnectDelaySeconds);
            }
            finally
            {
                await DisposeResourcesAsync();
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(options.Value.ReconnectDelaySeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task DisposeResourcesAsync()
    {
        var channel = _channel;
        _channel = null;
        if (channel is not null)
        {
            try
            {
                if (channel.IsOpen)
                {
                    await channel.CloseAsync();
                }

                await channel.DisposeAsync();
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "RabbitMQ consumer channel cleanup failed.");
            }
        }

        var connection = _connection;
        _connection = null;
        if (connection is not null)
        {
            try
            {
                if (connection.IsOpen)
                {
                    await connection.CloseAsync();
                }

                await connection.DisposeAsync();
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "RabbitMQ consumer connection cleanup failed.");
            }
        }
    }

    private Task HandleMessageAsync(object sender, BasicDeliverEventArgs delivery)
    {
        var channel = _channel ?? throw new InvalidOperationException("The RabbitMQ channel is not initialized.");
        return deliveryHandler.HandleAsync(
            delivery.BasicProperties.MessageId,
            delivery.Body.ToArray(),
            cancellationToken => channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken),
            cancellationToken => channel.BasicNackAsync(
                delivery.DeliveryTag,
                multiple: false,
                requeue: true,
                cancellationToken),
            delivery.CancellationToken);
    }
}
