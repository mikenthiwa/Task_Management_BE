using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Notifications.command.MarkAllAsRead;
using Application.Features.Notifications.Queries;
using Application.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using Task_Management_BE.Infrastructure;

namespace Task_Management_BE.Endpoints;

public class Notifications : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .AddFluentValidationAutoValidation()
            .MapGet(GetUserNotifications)
            .MapPost(MarkAllNotificationAsRead, "/mark-all-as-read");
    }
    
    private async Task<Results<Ok<Result<PaginatedList<NotificationDto>>>, BadRequest>> GetUserNotifications(
        ISender sender,
        ICurrentUserService currentUserService,
        [FromQuery(Name = "pageNumber")] int? pageNumber,
        [FromQuery(Name = "pageSize")] int? pageSize
        )
    {
        var userId = currentUserService.UserId;
        var query = new GetNotificationWithPaginationQuery()
        {
            PageNumber = pageNumber ?? 1, PageSize = pageSize ?? 10, UserId = userId!
        };
        var result = await sender.Send(query);
        return TypedResults.Ok(Result<PaginatedList<NotificationDto>>.SuccessResponse(200, "Notifications retrieved successfully", result));
    }
    
    private async Task<Results<Ok<Result>, BadRequest>> MarkAllNotificationAsRead(ISender sender, ICurrentUserService currentUserService)
    {
        var userId = currentUserService.UserId;
        var command = new MarkAllNotificationAsReadCommand() { UserId = userId! };
        await sender.Send(command);
        return TypedResults.Ok(Result.SuccessResponse(200, "All notifications marked as read"));
    }
}
