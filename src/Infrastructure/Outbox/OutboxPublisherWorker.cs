using Application.Common.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Outbox;

public sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<OutboxOptions> options,
    ILogger<OutboxPublisherWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextCleanupAt = timeProvider.GetUtcNow();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var publisher = scope.ServiceProvider.GetRequiredService<OutboxPublisher>();
                var publishedCount = await publisher.PublishBatchAsync(stoppingToken);

                if (timeProvider.GetUtcNow() >= nextCleanupAt)
                {
                    await publisher.DeleteProcessedMessagesAsync(stoppingToken);
                    nextCleanupAt = timeProvider.GetUtcNow().AddDays(1);
                }

                if (publishedCount == 0)
                {
                    await Task.Delay(options.Value.PollIntervalMilliseconds, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The outbox publisher iteration failed.");
                await Task.Delay(options.Value.PollIntervalMilliseconds, stoppingToken);
            }
        }
    }
}
