extern alias NotificationWorkerAssembly;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using INotificationDeliveryProcessor = NotificationWorkerAssembly::NotificationWorker.INotificationDeliveryProcessor;
using NotificationDeliveryHandler = NotificationWorkerAssembly::NotificationWorker.NotificationDeliveryHandler;
using NotificationProcessingResult = NotificationWorkerAssembly::NotificationWorker.NotificationProcessingResult;

namespace Application.FunctionalTests.Notifications;

public class NotificationDeliveryHandlerTests
{
    [Fact]
    public async Task ShouldAcknowledgeSuccessfulProcessing()
    {
        var processor = new StubProcessor(NotificationProcessingResult.Success());
        var messageBus = new NoOpMessageBus();
        var handler = CreateHandler(processor, messageBus);
        var acknowledged = 0;
        var requeued = 0;

        await handler.HandleAsync(
            Guid.NewGuid().ToString(),
            [],
            _ =>
            {
                acknowledged++;
                return ValueTask.CompletedTask;
            },
            _ =>
            {
                requeued++;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        acknowledged.Should().Be(1);
        requeued.Should().Be(0);
        messageBus.PublishedMessageIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldDeadLetterThenAcknowledgePermanentFailure()
    {
        var processor = new StubProcessor(NotificationProcessingResult.Failure("invalid payload"));
        var messageBus = new NoOpMessageBus();
        var handler = CreateHandler(processor, messageBus);
        var messageId = Guid.NewGuid();
        var acknowledged = 0;

        await handler.HandleAsync(
            messageId.ToString(),
            [1, 2, 3],
            _ =>
            {
                acknowledged++;
                return ValueTask.CompletedTask;
            },
            _ => ValueTask.CompletedTask,
            CancellationToken.None);

        messageBus.PublishedMessageIds.Should().Equal(messageId);
        acknowledged.Should().Be(1);
    }

    [Fact]
    public async Task ShouldRequeueWhenDeadLetterPublishFails()
    {
        var processor = new StubProcessor(NotificationProcessingResult.Failure("API unavailable"));
        var messageBus = new NoOpMessageBus { ThrowOnPublish = true };
        var handler = CreateHandler(processor, messageBus);
        var acknowledged = 0;
        var requeued = 0;

        await handler.HandleAsync(
            Guid.NewGuid().ToString(),
            [],
            _ =>
            {
                acknowledged++;
                return ValueTask.CompletedTask;
            },
            _ =>
            {
                requeued++;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        acknowledged.Should().Be(0);
        requeued.Should().Be(1);
    }

    [Fact]
    public async Task ShouldDeadLetterMessageWithMissingMessageIdWithoutProcessingIt()
    {
        var processor = new StubProcessor(NotificationProcessingResult.Success());
        var messageBus = new NoOpMessageBus();
        var handler = CreateHandler(processor, messageBus);
        var acknowledged = 0;

        await handler.HandleAsync(
            null,
            [],
            _ =>
            {
                acknowledged++;
                return ValueTask.CompletedTask;
            },
            _ => ValueTask.CompletedTask,
            CancellationToken.None);

        processor.CallCount.Should().Be(0);
        messageBus.PublishedMessageIds.Should().ContainSingle();
        acknowledged.Should().Be(1);
    }

    [Fact]
    public async Task ShouldLeaveMessageUnacknowledgedWhenProcessingIsInterrupted()
    {
        var messageBus = new NoOpMessageBus();
        var handler = CreateHandler(new InterruptedProcessor(), messageBus);
        var acknowledged = 0;
        var requeued = 0;

        var act = () => handler.HandleAsync(
            Guid.NewGuid().ToString(),
            [],
            _ =>
            {
                acknowledged++;
                return ValueTask.CompletedTask;
            },
            _ =>
            {
                requeued++;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<OperationCanceledException>();
        acknowledged.Should().Be(0);
        requeued.Should().Be(0);
    }

    private static NotificationDeliveryHandler CreateHandler(
        INotificationDeliveryProcessor processor,
        NoOpMessageBus messageBus)
    {
        return new NotificationDeliveryHandler(
            processor,
            messageBus,
            TimeProvider.System,
            NullLogger<NotificationDeliveryHandler>.Instance);
    }

    private sealed class StubProcessor(NotificationProcessingResult result) : INotificationDeliveryProcessor
    {
        public int CallCount { get; private set; }

        public Task<NotificationProcessingResult> ProcessAsync(
            Guid messageId,
            ReadOnlyMemory<byte> payload,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(result);
        }
    }

    private sealed class InterruptedProcessor : INotificationDeliveryProcessor
    {
        public Task<NotificationProcessingResult> ProcessAsync(
            Guid messageId,
            ReadOnlyMemory<byte> payload,
            CancellationToken cancellationToken)
        {
            throw new OperationCanceledException("Simulated worker interruption.");
        }
    }
}
