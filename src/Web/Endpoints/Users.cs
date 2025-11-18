using Application.Features.Users.Queries;
using Application.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Task_Management_BE.Infrastructure;

namespace Task_Management_BE.Endpoints;

public class Users : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapGet("/users", GetUsers);
    }

    public async Task<Results<Ok<Result<List<UserDto>>>, BadRequest>> GetUsers(ISender sender)
    {
        var query = new GetUsersQuery();
        var result = await sender.Send(query);
        return TypedResults.Ok(Result<List<UserDto>>.SuccessResponse(200, "User retrieved successfully", result));
    }
}
