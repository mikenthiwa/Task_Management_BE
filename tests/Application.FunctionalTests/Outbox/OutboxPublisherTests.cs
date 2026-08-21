using Application.Common.Models;
using Application.Common.Options;
using Domain.Events;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskEntity = Domain.Entities.Task;

namespace Application.FunctionalTests.Outbox;

public class OutboxPublisherTests(CustomWebApplicationFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task ShouldNotSaveWhenNoMessagesAreClaimed()
    {
        using var scope = Factory.Services.CreateScope();
        var existingDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var messageBus = Factory.Services.GetRequiredService<NoOpMessageBus>();
        messageBus.Reset();
        var interceptor = new SaveChangesCounterInterceptor();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(existingDbContext.Database.GetConnectionString())
            .AddInterceptors(interceptor)
            .Options;
        await using var dbContext = new ApplicationDbContext(options);

        var publishedCount = await CreatePublisher(dbContext, messageBus)
            .PublishBatchAsync(CancellationToken.None);

        publishedCount.Should().Be(0);
        messageBus.PublishedMessageIds.Should().BeEmpty();
        interceptor.SavingChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task ShouldPublishPendingMessageWithStableIdAndMarkItProcessed()
    {
        var marker = Guid.NewGuid().ToString();
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var messageBus = Factory.Services.GetRequiredService<NoOpMessageBus>();
        messageBus.Reset();
        var task = new TaskEntity
        {
            Title = $"Outbox publisher test {marker}",
            CreatorId = "test-user-id"
        };
        task.AddDomainEvent(new TaskCreatedEvent(task));
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync();
        var outboxMessages = await dbContext.OutboxMessages.ToListAsync();
        var outboxMessage = outboxMessages.Single(message => message.Payload.Contains(marker));

        await CreatePublisher(dbContext, messageBus).PublishBatchAsync(CancellationToken.None);

        messageBus.PublishedMessageIds.Should().Contain(outboxMessage.Id);
        outboxMessage.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldScheduleRetryWhenRabbitMqPublishFails()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var messageBus = Factory.Services.GetRequiredService<NoOpMessageBus>();
        messageBus.Reset();
        messageBus.ThrowOnPublish = true;
        var message = CreateOutboxMessage();
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        await CreatePublisher(dbContext, messageBus).PublishBatchAsync(CancellationToken.None);

        message.ProcessedAt.Should().BeNull();
        message.FailedAt.Should().BeNull();
        message.Attempts.Should().Be(1);
        message.NextAttemptAt.Should().BeAfter(message.OccurredAt);
    }

    [Fact]
    public async Task ShouldMarkUnsupportedMessageTypeAsPermanentlyFailed()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var messageBus = Factory.Services.GetRequiredService<NoOpMessageBus>();
        messageBus.Reset();
        var message = new OutboxMessage(
            "Unsupported.Message",
            "{}",
            "task.events",
            "task.unsupported",
            DateTimeOffset.UtcNow);
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        await CreatePublisher(dbContext, messageBus).PublishBatchAsync(CancellationToken.None);

        message.FailedAt.Should().NotBeNull();
        message.ProcessedAt.Should().BeNull();
        messageBus.PublishedMessageIds.Should().NotContain(message.Id);
    }

    [Fact]
    public async Task ShouldMarkInvalidPayloadAsPermanentlyFailed()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var messageBus = Factory.Services.GetRequiredService<NoOpMessageBus>();
        messageBus.Reset();
        var message = new OutboxMessage(
            typeof(NotificationIntegrationEvent).FullName!,
            "{}",
            "task.events",
            "task.created",
            DateTimeOffset.UtcNow);
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        await CreatePublisher(dbContext, messageBus).PublishBatchAsync(CancellationToken.None);

        message.FailedAt.Should().NotBeNull();
        message.ProcessedAt.Should().BeNull();
        messageBus.PublishedMessageIds.Should().NotContain(message.Id);
    }

    [Fact]
    public async Task ShouldReclaimMessageAfterLeaseExpires()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var messageBus = Factory.Services.GetRequiredService<NoOpMessageBus>();
        messageBus.Reset();
        var message = CreateOutboxMessage();
        message.AcquireLease(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-1));
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        await CreatePublisher(dbContext, messageBus).PublishBatchAsync(CancellationToken.None);

        message.ProcessedAt.Should().NotBeNull();
        message.LeaseId.Should().BeNull();
        messageBus.PublishedMessageIds.Should().Contain(message.Id);
    }

    [Fact]
    public async Task ShouldRenewLeaseWhilePublicationIsInProgress()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var messageBus = Factory.Services.GetRequiredService<NoOpMessageBus>();
        messageBus.Reset();
        var publishStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releasePublish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        messageBus.BeforePublishAsync = async cancellationToken =>
        {
            publishStarted.TrySetResult();
            await releasePublish.Task.WaitAsync(cancellationToken);
        };
        var message = CreateOutboxMessage();
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();
        var publisher = CreatePublisher(
            dbContext,
            messageBus,
            new OutboxOptions { LeaseSeconds = 2 });

        var publishTask = publisher.PublishBatchAsync(CancellationToken.None);
        try
        {
            await publishStarted.Task;
            await Task.Delay(TimeSpan.FromMilliseconds(1_500));

            using var verificationScope = Factory.Services.CreateScope();
            var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var leasedUntil = await verificationDbContext.OutboxMessages
                .Where(item => item.Id == message.Id)
                .Select(item => item.LeasedUntil)
                .SingleAsync();
            leasedUntil.Should().BeAfter(DateTimeOffset.UtcNow);
        }
        finally
        {
            releasePublish.TrySetResult();
        }

        await publishTask;
    }

    [Fact]
    public async Task ShouldDeleteProcessedMessagesAfterRetentionPeriod()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var messageBus = Factory.Services.GetRequiredService<NoOpMessageBus>();
        messageBus.Reset();
        var message = CreateOutboxMessage();
        message.MarkProcessed(DateTimeOffset.UtcNow.AddDays(-8));
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        await CreatePublisher(dbContext, messageBus).DeleteProcessedMessagesAsync(CancellationToken.None);

        (await dbContext.OutboxMessages.AnyAsync(item => item.Id == message.Id)).Should().BeFalse();
    }

    private OutboxPublisher CreatePublisher(
        ApplicationDbContext dbContext,
        NoOpMessageBus messageBus,
        OutboxOptions? outboxOptions = null)
    {
        return new OutboxPublisher(
            dbContext,
            messageBus,
            Factory.Services.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            Options.Create(outboxOptions ?? new OutboxOptions()),
            NullLogger<OutboxPublisher>.Instance);
    }

    private static OutboxMessage CreateOutboxMessage()
    {
        var notification = new NotificationIntegrationEvent
        {
            UserId = "test-user-id",
            Message = "Test notification",
            Type = Domain.Enum.NotificationType.TaskCreated
        };
        return new OutboxMessage(
            typeof(NotificationIntegrationEvent).FullName!,
            System.Text.Json.JsonSerializer.Serialize(notification),
            "task.events",
            "task.created",
            DateTimeOffset.UtcNow);
    }

    private sealed class SaveChangesCounterInterceptor : SaveChangesInterceptor
    {
        public int SavingChangesCalls { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            SavingChangesCalls++;
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
