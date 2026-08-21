using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Common.Models;
using Application.Common.Options;
using Microsoft.Extensions.Options;

namespace NotificationWorker;

public sealed class NotificationDeliveryProcessor(
    IHttpClientFactory httpClientFactory,
    IOptions<NotificationConsumerOptions> options) : INotificationDeliveryProcessor
{
    public async Task<NotificationProcessingResult> ProcessAsync(
        Guid messageId,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        NotificationIntegrationEvent? message;
        try
        {
            message = JsonSerializer.Deserialize<NotificationIntegrationEvent>(payload.Span)
                ?? throw new JsonException("The notification payload was null.");
        }
        catch (JsonException exception)
        {
            return NotificationProcessingResult.Failure(exception.Message);
        }

        string? lastError = null;
        for (var attempt = 1; attempt <= options.Value.MaximumAttempts; attempt++)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient("web");
                using var response = await httpClient.PostAsJsonAsync(
                    "api/NotificationsInternal/internal/notifications",
                    new
                    {
                        MessageId = messageId,
                        message.UserId,
                        message.Message,
                        message.Type,
                        message.ActionUrl,
                        message.ActionLabel
                    },
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return NotificationProcessingResult.Success();
                }

                lastError = $"Notification API returned HTTP {(int)response.StatusCode}.";
                if (!IsTransient(response.StatusCode))
                {
                    return NotificationProcessingResult.Failure(lastError);
                }
            }
            catch (HttpRequestException exception)
            {
                lastError = exception.Message;
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                lastError = exception.Message;
            }

            if (attempt < options.Value.MaximumAttempts)
            {
                await Task.Delay(GetRetryDelay(attempt), cancellationToken);
            }
        }

        return NotificationProcessingResult.Failure(lastError ?? "Notification processing failed.");
    }

    private TimeSpan GetRetryDelay(int attempt)
    {
        if (options.Value.RetryDelaySeconds.Length == 0)
        {
            return TimeSpan.Zero;
        }

        var index = Math.Min(attempt - 1, options.Value.RetryDelaySeconds.Length - 1);
        return TimeSpan.FromSeconds(options.Value.RetryDelaySeconds[index]);
    }

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
               || (int)statusCode >= 500;
    }
}
