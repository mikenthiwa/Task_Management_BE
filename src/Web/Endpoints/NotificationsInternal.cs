using Application.Common.Interfaces;
using Application.Models;
using Domain.Enum;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Task_Management_BE.Infrastructure;

namespace Task_Management_BE.Endpoints;

public sealed record CreateNotificationRequest(
    string UserId,
    string Message,
    NotificationType Type,
    string? ActionUrl,
    string? ActionLabel);

public class NotificationsInternal : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .AllowAnonymous()
            .MapPost(CreateNotification, "/internal/notifications");
    }

    static private async Task<Results<Ok<Result>, UnauthorizedHttpResult>> CreateNotification(
        [FromBody] CreateNotificationRequest request,
        INotificationService notificationService,
        IConfiguration configuration,
        HttpRequest httpRequest)
    {
        var apiKey = configuration["WorkerApiKey"];
        var headerKey = httpRequest.Headers["X-Worker-Key"].ToString();

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey != headerKey)
        {
            return TypedResults.Unauthorized();
        }

        await notificationService.CreateNotificationAsync(
            request.UserId,
            request.Message,
            request.Type,
            request.ActionUrl,
            request.ActionLabel);

        return TypedResults.Ok(Result.SuccessResponse(200, "Notification created"));
    }
}
