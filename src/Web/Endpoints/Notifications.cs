using Application.Common.Models;
using Application.Features.Notifications.Queries;
using Application.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Task_Management_BE.Infrastructure;

namespace Task_Management_BE.Endpoints;

public class Notifications : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        // Define notification-related endpoints here
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetUserNotifications, "/user/{userId}");

    }
    
    public async Task<Results<Ok<Result<PaginatedList<NotificationDto>>>, BadRequest>> GetUserNotifications(
        [FromRoute] string userId,
        ISender sender, 
        [FromQuery(Name = "pageNumber")] int? pageNumber,
        [FromQuery(Name = "pageSize")] int? pageSize
        )
    {
        // Implementation for fetching user notifications
        var query = new GetNotificationWithPaginationQuery()
        {
            UserId = userId, PageNumber = pageNumber ?? 1, PageSize = pageSize ?? 10
        };
        var result = await sender.Send(query);
        return TypedResults.Ok(Result<PaginatedList<NotificationDto>>.SuccessResponse(200, "Notifications retrieved successfully", result));
    }
}
