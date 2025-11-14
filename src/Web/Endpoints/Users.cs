using Application.Features.Users.Command.OauthLogin;
using Application.Features.Users.Queries;
using Application.Models;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Task_Management_BE.Infrastructure;

namespace Task_Management_BE.Endpoints;

public class Users : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        // app.MapGroup(this)
        //     .MapIdentityApi<ApplicationUser>();

        app.MapGroup(this)
            .MapGet("/users", GetUsers);
        app.MapGroup(this).MapPost("/google", Google);
    }

    public async Task<Results<Ok<Result<List<UserDto>>>, BadRequest>> GetUsers(ISender sender)
    {
        var query = new GetUsersQuery();
        var result = await sender.Send(query);
        return TypedResults.Ok(Result<List<UserDto>>.SuccessResponse(200, "User retrieved successfully", result));
    }

    public async Task<Results<Ok<Result<TokenSetDto>>, BadRequest>> Google(ISender sender, OauthLoginCommand command)
    {
        var result = await sender.Send(command);
        return TypedResults.Ok(Result<TokenSetDto>.SuccessResponse(200, "Login successful", result));
    }
    
        
    
}
