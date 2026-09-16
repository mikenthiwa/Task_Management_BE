using Application.Common.Interfaces;
using Domain.Enum;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Task_Management_BE.Infrastructure;

namespace Task_Management_BE.Endpoints;

/// <summary>Creates a notification from a RabbitMQ delivery.</summary>
public sealed record CreateNotificationRequest(
    Guid MessageId,
    string UserId,
    string Message,
    NotificationType Type,
    string? ActionUrl,
    string? ActionLabel);

/// <summary>Identifies the stored notification and whether this delivery created it.</summary>
public sealed record CreateNotificationResponse(Guid NotificationId, bool Created);

public class NotificationsInternal : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .AllowAnonymous()
            .MapPost(CreateNotification, "/internal/notifications");
    }

    private static async Task<Results<Ok<CreateNotificationResponse>, UnauthorizedHttpResult, BadRequest<ProblemDetails>>> CreateNotification(
        [FromBody] CreateNotificationRequest request,
        INotificationService notificationService,
        IConfiguration configuration,
        HttpRequest httpRequest,
        CancellationToken cancellationToken)
    {
        var apiKey = configuration["WorkerApiKey"];
        var headerKey = httpRequest.Headers["X-Worker-Key"].ToString();

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey != headerKey)
        {
            return TypedResults.Unauthorized();
        }

        if (request.MessageId == Guid.Empty)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid message ID",
                Detail = "MessageId must be a non-empty GUID.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (string.IsNullOrWhiteSpace(request.UserId)
            || string.IsNullOrWhiteSpace(request.Message)
            || !Enum.IsDefined(request.Type)
            || (string.IsNullOrWhiteSpace(request.ActionUrl) != string.IsNullOrWhiteSpace(request.ActionLabel)))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid notification",
                Detail = "UserId, Message, Type, and any notification action must be valid.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var result = await notificationService.CreateNotificationAsync(
            request.MessageId,
            request.UserId,
            request.Message,
            request.Type,
            request.ActionUrl,
            request.ActionLabel,
            cancellationToken);

        return TypedResults.Ok(new CreateNotificationResponse(result.NotificationId, result.Created));
    }
}
