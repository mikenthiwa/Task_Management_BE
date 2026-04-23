using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace Task_Management_BE.HealthChecks;

public sealed class RabbitMqHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMq:HostName"] ?? "localhost",
                UserName = configuration["RabbitMq:UserName"] ?? "admin",
                Password = configuration["RabbitMq:Password"] ?? "admin",
                VirtualHost = configuration["RabbitMq:VirtualHost"] ?? "/",
                Port = configuration.GetValue<int?>("RabbitMq:Port") ?? 5672
            };

            cancellationToken.ThrowIfCancellationRequested();

            using var connection = await factory.CreateConnectionAsync();

            return connection.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ is reachable.")
                : HealthCheckResult.Unhealthy("RabbitMQ connection is closed.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ is unreachable.", exception);
        }
    }
}
