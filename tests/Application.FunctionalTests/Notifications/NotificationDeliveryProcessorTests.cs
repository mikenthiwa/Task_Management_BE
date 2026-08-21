extern alias NotificationWorkerAssembly;

using System.Net;
using System.Text.Json;
using Application.Common.Models;
using Application.Common.Options;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NotificationDeliveryProcessor = NotificationWorkerAssembly::NotificationWorker.NotificationDeliveryProcessor;

namespace Application.FunctionalTests.Notifications;

public class NotificationDeliveryProcessorTests
{
    [Fact]
    public async Task ShouldRejectInvalidJsonWithoutCallingTheApi()
    {
        var handler = new SequenceHttpMessageHandler(HttpStatusCode.OK);
        var processor = CreateProcessor(handler);

        var result = await processor.ProcessAsync(
            Guid.NewGuid(),
            "not-json"u8.ToArray(),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        handler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task ShouldRetryTransientResponsesAndEventuallySucceed()
    {
        var handler = new SequenceHttpMessageHandler(
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.OK);
        var processor = CreateProcessor(handler);

        var result = await processor.ProcessAsync(
            Guid.NewGuid(),
            SerializeNotification(),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        handler.RequestCount.Should().Be(3);
    }

    [Fact]
    public async Task ShouldNotRetryPermanentClientError()
    {
        var handler = new SequenceHttpMessageHandler(HttpStatusCode.BadRequest);
        var processor = CreateProcessor(handler);

        var result = await processor.ProcessAsync(
            Guid.NewGuid(),
            SerializeNotification(),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task ShouldStopAfterMaximumTransientAttempts()
    {
        var handler = new SequenceHttpMessageHandler(
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.ServiceUnavailable);
        var processor = CreateProcessor(handler);

        var result = await processor.ProcessAsync(
            Guid.NewGuid(),
            SerializeNotification(),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        handler.RequestCount.Should().Be(3);
    }

    [Fact]
    public async Task ShouldRetryHttpTimeoutAndEventuallySucceed()
    {
        var handler = new TimeoutThenSuccessHttpMessageHandler();
        var processor = CreateProcessor(handler);

        var result = await processor.ProcessAsync(
            Guid.NewGuid(),
            SerializeNotification(),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        handler.RequestCount.Should().Be(2);
    }

    private static NotificationDeliveryProcessor CreateProcessor(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        return new NotificationDeliveryProcessor(
            new StubHttpClientFactory(client),
            Options.Create(new NotificationConsumerOptions
            {
                MaximumAttempts = 3,
                RetryDelaySeconds = [0, 0]
            }));
    }

    private static byte[] SerializeNotification()
    {
        return JsonSerializer.SerializeToUtf8Bytes(new NotificationIntegrationEvent
        {
            UserId = "test-user-id",
            Message = "Test notification",
            Type = Domain.Enum.NotificationType.TaskCreated
        });
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class SequenceHttpMessageHandler(params HttpStatusCode[] statusCodes) : HttpMessageHandler
    {
        private readonly Queue<HttpStatusCode> _statusCodes = new(statusCodes);

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            var statusCode = _statusCodes.Count > 0 ? _statusCodes.Dequeue() : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }

    private sealed class TimeoutThenSuccessHttpMessageHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            if (RequestCount == 1)
            {
                throw new TaskCanceledException("Simulated HTTP timeout.");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
