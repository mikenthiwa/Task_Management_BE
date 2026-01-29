using System.Text.Json;
using System.Net.Http.Json;
using Application.Features.Tasks.IntegrationEvent;
using Domain.Enum;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationWorker;

public class Worker(IServiceProvider serviceProvider, string hostName, string userName, string password) : BackgroundService
{
    private Task<IConnection>? _connection;
    private Task<IChannel>? _channel;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = hostName, UserName = userName, Password = password };
        _connection = factory.CreateConnectionAsync(cancellationToken: stoppingToken);
        _channel = _connection.Result.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.Result.ExchangeDeclareAsync(exchange: "task.events", type: ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: stoppingToken);
        QueueDeclareOk queueDeclareResult = await _channel.Result.QueueDeclareAsync("notification",durable:false, exclusive:false, autoDelete:false, cancellationToken: stoppingToken);
        string queueName = queueDeclareResult.QueueName;
        await _channel.Result.QueueBindAsync(queue: queueName, exchange: "task.events",
            routingKey: "task.*", cancellationToken: stoppingToken);
        var consumer = new AsyncEventingBasicConsumer(_channel.Result);
        consumer.ReceivedAsync += HandleMessageAsync;
        await _channel.Result.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer, cancellationToken: stoppingToken);
    }
    
    public async Task HandleMessageAsync(object model, BasicDeliverEventArgs ea)
    {
        byte[] body = ea.Body.ToArray();
        var message = JsonSerializer.Deserialize<TaskCreatedIntegrationEvent>(body)!;
        using var scope = serviceProvider.CreateScope();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("web");
        var messageDescription = $"Task '{message.Title}' has been created.";

        var response = await httpClient.PostAsJsonAsync("api/NotificationsInternal/internal/notifications", new
        {
            UserId = message.CreatorId!,
            Message = messageDescription,
            Type = (int)NotificationType.TaskCreated,
            ActionUrl = (string?)null,
            ActionLabel = (string?)null
        });

        response.EnsureSuccessStatusCode();
    }
}
