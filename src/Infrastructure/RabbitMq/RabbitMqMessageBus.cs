using System.Text.Json;
using Application.Common.Interfaces;
using RabbitMQ.Client;

namespace Infrastructure.RabbitMq;

public sealed class RabbitMqMessageBus : IMessageBus, IAsyncDisposable
{
    private readonly object _resourcesSync = new();
    private readonly SemaphoreSlim _publishLock = new(1, 1);
    private readonly string _hostName;
    private readonly string _userName;
    private readonly string _password;
    private readonly string _virtualHost;
    private readonly int _port;
    private readonly bool _useSsl;
    private Lazy<Task<RabbitMqResources>> _resources;
    private bool _disposed;

    public RabbitMqMessageBus(string hostName, string userName, string password, string virtualHost, int port, bool useSsl)
    {
        _hostName = hostName;
        _userName = userName;
        _password = password;
        _virtualHost = virtualHost;
        _port = port;
        _useSsl = useSsl;
        _resources = CreateResourcesHolder();
    }

    public async Task PublishAsync<T>(
        T message,
        string exchange,
        string routingKey,
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        await _publishLock.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var resourcesHolder = _resources;
            try
            {
                var resources = await resourcesHolder.Value;
                var props = new BasicProperties
                {
                    Persistent = true,
                    MessageId = messageId.ToString(),
                    ContentType = "application/json",
                    Type = typeof(T).FullName
                };
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                await resources.Channel.BasicPublishAsync(
                    exchange,
                    routingKey,
                    mandatory: true,
                    basicProperties: props,
                    body,
                    cancellationToken);
            }
            catch
            {
                await ResetResourcesAsync(resourcesHolder);
                throw;
            }
        }
        finally
        {
            _publishLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _publishLock.WaitAsync();
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            await DisposeResourcesSafelyAsync(_resources);
        }
        finally
        {
            _publishLock.Release();
            _publishLock.Dispose();
        }
    }

    private static async Task<RabbitMqResources> CreateResourcesAsync(
        string hostName,
        string userName,
        string password,
        string virtualHost,
        int port,
        bool useSsl)
    {
        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password,
            VirtualHost = virtualHost,
            Port = port,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };
        if (useSsl)
        {
            factory.Ssl = new SslOption { Enabled = true, ServerName = hostName };
        }

        var connection = await factory.CreateConnectionAsync();
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        var channel = await connection.CreateChannelAsync(channelOptions);
        await RabbitMqTopology.DeclareAsync(channel);
        return new RabbitMqResources(connection, channel);
    }

    private Lazy<Task<RabbitMqResources>> CreateResourcesHolder()
    {
        return new Lazy<Task<RabbitMqResources>>(() =>
            CreateResourcesAsync(_hostName, _userName, _password, _virtualHost, _port, _useSsl));
    }

    private async Task ResetResourcesAsync(Lazy<Task<RabbitMqResources>> failedResources)
    {
        var shouldDispose = false;
        lock (_resourcesSync)
        {
            if (ReferenceEquals(_resources, failedResources))
            {
                _resources = CreateResourcesHolder();
                shouldDispose = true;
            }
        }

        if (shouldDispose)
        {
            await DisposeResourcesSafelyAsync(failedResources);
        }
    }

    private static async Task DisposeResourcesSafelyAsync(Lazy<Task<RabbitMqResources>> resourcesHolder)
    {
        if (!resourcesHolder.IsValueCreated)
        {
            return;
        }

        try
        {
            var resources = await resourcesHolder.Value;
            if (resources.Channel.IsOpen)
            {
                await resources.Channel.CloseAsync();
            }

            await resources.Channel.DisposeAsync();
            if (resources.Connection.IsOpen)
            {
                await resources.Connection.CloseAsync();
            }

            await resources.Connection.DisposeAsync();
        }
        catch
        {
            // A failed RabbitMQ resource is best-effort cleanup; the next publish creates a fresh one.
        }
    }

    private sealed record RabbitMqResources(IConnection Connection, IChannel Channel);
}
