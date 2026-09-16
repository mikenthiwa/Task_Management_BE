using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Common.Options;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Outbox;

public sealed class OutboxPublisher(
    ApplicationDbContext dbContext,
    IMessageBus messageBus,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<OutboxOptions> options,
    ILogger<OutboxPublisher> logger)
{
    public async Task<int> PublishBatchAsync(CancellationToken cancellationToken)
    {
        var batch = await ClaimBatchAsync(cancellationToken);
        if (batch.Messages.Count == 0)
        {
            return 0;
        }

        using var renewalFailed = new CancellationTokenSource();
        using var processing = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            renewalFailed.Token);
        var leaseRenewalTask = RenewLeaseAsync(batch, renewalFailed, processing.Token);
        try
        {
            foreach (var message in batch.Messages)
            {
                await PublishMessageAsync(message, processing.Token);
            }
        }
        finally
        {
            await processing.CancelAsync();
            try
            {
                await leaseRenewalTask;
            }
            catch (OperationCanceledException) when (processing.IsCancellationRequested)
            {
            }
        }

        return batch.Messages.Count;
    }

    public Task<int> DeleteProcessedMessagesAsync(CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow().AddDays(-options.Value.ProcessedRetentionDays);
        return dbContext.OutboxMessages
            .Where(message => message.ProcessedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<ClaimedBatch> ClaimBatchAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var leaseId = Guid.NewGuid();
        var leasedUntil = now.AddSeconds(options.Value.LeaseSeconds);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var messages = await dbContext.OutboxMessages
            .FromSqlInterpolated($$"""
                SELECT *
                FROM "OutboxMessages"
                WHERE "ProcessedAt" IS NULL
                  AND "FailedAt" IS NULL
                  AND ("NextAttemptAt" IS NULL OR "NextAttemptAt" <= {{now}})
                  AND ("LeasedUntil" IS NULL OR "LeasedUntil" < {{now}})
                ORDER BY "OccurredAt"
                LIMIT {{options.Value.BatchSize}}
                FOR UPDATE SKIP LOCKED
            """)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return new ClaimedBatch(messages, leaseId);
        }

        foreach (var message in messages)
        {
            message.AcquireLease(leaseId, leasedUntil);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ClaimedBatch(messages, leaseId);
    }

    private async Task RenewLeaseAsync(
        ClaimedBatch batch,
        CancellationTokenSource renewalFailed,
        CancellationToken cancellationToken)
    {
        var renewalInterval = TimeSpan.FromSeconds(Math.Max(options.Value.LeaseSeconds / 2d, 0.1));
        var messageIds = batch.Messages.Select(message => message.Id).ToArray();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(renewalInterval, cancellationToken);
                await using var scope = scopeFactory.CreateAsyncScope();
                var renewalDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var leasedUntil = timeProvider.GetUtcNow().AddSeconds(options.Value.LeaseSeconds);
                await renewalDbContext.OutboxMessages
                    .Where(message => messageIds.Contains(message.Id)
                                      && message.LeaseId == batch.LeaseId
                                      && message.ProcessedAt == null
                                      && message.FailedAt == null)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(message => message.LeasedUntil, leasedUntil),
                        cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            await renewalFailed.CancelAsync();
            throw;
        }
    }

    private async Task PublishMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            if (message.Type != typeof(NotificationIntegrationEvent).FullName)
            {
                message.MarkPermanentlyFailed(
                    timeProvider.GetUtcNow(),
                    $"Unsupported outbox message type '{message.Type}'.");
            }
            else
            {
                var notification = JsonSerializer.Deserialize<NotificationIntegrationEvent>(message.Payload)
                    ?? throw new JsonException("The notification payload was null.");
                ValidateNotification(notification);
                await messageBus.PublishAsync(
                    notification,
                    message.Exchange,
                    message.RoutingKey,
                    message.Id,
                    cancellationToken);
                message.MarkProcessed(timeProvider.GetUtcNow());
            }
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "Outbox message {MessageId} has an invalid payload.", message.Id);
            message.MarkPermanentlyFailed(timeProvider.GetUtcNow(), exception.Message);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var retryDelay = CalculateRetryDelay(message.Attempts);
            logger.LogWarning(
                exception,
                "Publishing outbox message {MessageId} failed; retrying at {NextAttemptAt}.",
                message.Id,
                timeProvider.GetUtcNow().Add(retryDelay));
            message.ScheduleRetry(timeProvider.GetUtcNow().Add(retryDelay), exception.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private TimeSpan CalculateRetryDelay(int attempts)
    {
        var seconds = options.Value.InitialRetryDelaySeconds * Math.Pow(2, attempts);
        return TimeSpan.FromSeconds(Math.Min(seconds, options.Value.MaximumRetryDelaySeconds));
    }

    private static void ValidateNotification(NotificationIntegrationEvent notification)
    {
        if (string.IsNullOrWhiteSpace(notification.UserId)
            || string.IsNullOrWhiteSpace(notification.Message)
            || !Enum.IsDefined(notification.Type))
        {
            throw new JsonException("The notification payload is missing required or valid values.");
        }

        var hasActionUrl = !string.IsNullOrWhiteSpace(notification.ActionUrl);
        var hasActionLabel = !string.IsNullOrWhiteSpace(notification.ActionLabel);
        if (hasActionUrl != hasActionLabel)
        {
            throw new JsonException("The notification action URL and label must be supplied together.");
        }
    }

    private sealed record ClaimedBatch(List<OutboxMessage> Messages, Guid LeaseId);
}
