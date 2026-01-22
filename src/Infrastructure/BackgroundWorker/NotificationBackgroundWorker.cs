using System.Text.Json;
using Application.Common.Interfaces;
using Application.Features.Tasks.IntegrationEvent;
using Domain.Enum;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure.BackgroundWorker;

public class NotificationBackgroundWorker(IServiceProvider serviceProvider) : BackgroundService
{
    private Task<IConnection>? _connection;
    private Task<IChannel>? _channel;
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Placeholder for future notification processing logic
        var factory = new ConnectionFactory { HostName = "localhost", UserName = "admin", Password = "admin"};
        _connection = factory.CreateConnectionAsync(cancellationToken: stoppingToken);
        _channel = _connection.Result.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.Result.ExchangeDeclareAsync(exchange: "task.events", type: ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: stoppingToken);
        QueueDeclareOk queueDeclareResult = await _channel.Result.QueueDeclareAsync("notification.task.created" ,cancellationToken: stoppingToken);
        string queueName = queueDeclareResult.QueueName;
        await _channel.Result.QueueBindAsync(queue: queueName, exchange: "task.events",
            routingKey: "task.created", cancellationToken: stoppingToken);
        var consumer = new AsyncEventingBasicConsumer(_channel.Result);
        consumer.ReceivedAsync += HandleMessageAsync;
        await _channel.Result.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer, cancellationToken: stoppingToken);
    }
    
    public async Task HandleMessageAsync(object model, BasicDeliverEventArgs ea)
    {
        // Process the message and create notifications as needed
        byte[] body = ea.Body.ToArray();
        var message = JsonSerializer.Deserialize<TaskCreatedIntegrationEvent>(body)!;
        Console.WriteLine(" [x] Received {0}", message);
        using var scope = serviceProvider.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var messageDescription = $"Task '{message.Title}' has been created.";
        await notificationService.CreateNotificationAsync(message.CreatorId!, messageDescription,
            NotificationType.TaskCreated, null, null);
    }
}
