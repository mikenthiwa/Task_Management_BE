using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Application.FunctionalTests.Notifications;

public class InternalNotificationTests(CustomWebApplicationFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task ShouldCreateOneNotificationAndRepublishItForDuplicateMessage()
    {
        var messageId = Guid.NewGuid();
        var publisher = Factory.Services.GetRequiredService<RecordingNotificationPublisherService>();
        publisher.Clear();

        var firstResponse = await PostNotificationAsync(messageId);
        var secondResponse = await PostNotificationAsync(messageId);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notification = await dbContext.Notifications.SingleAsync(item => item.SourceMessageId == messageId);

        publisher.PublishedNotificationIds.Should().Equal(notification.Id, notification.Id);
    }

    private Task<HttpResponseMessage> PostNotificationAsync(Guid messageId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/NotificationsInternal/internal/notifications")
        {
            Content = JsonContent.Create(new
            {
                MessageId = messageId,
                UserId = "test-user-id",
                Message = "A task was created",
                Type = Domain.Enum.NotificationType.TaskCreated,
                ActionUrl = (string?)null,
                ActionLabel = (string?)null
            })
        };
        request.Headers.Add("X-Worker-Key", "change-me");

        return HttpClient.SendAsync(request);
    }
}
