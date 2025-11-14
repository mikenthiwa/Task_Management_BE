using Application.Features.Users.Command.Oauth;
using Task_Management_BE.Infrastructure;
using Application.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Task_Management_BE.Endpoints;

public class Auth : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this).MapPost("/social-login", Oauth);
    }
    
    private async Task<Results<Ok<Result<TokenSetDto>>, BadRequest>> Oauth(ISender sender, OauthCommand command)
    {
        var result = await sender.Send(command);
        return TypedResults.Ok(Result<TokenSetDto>.SuccessResponse(200, "Login successful", result));
    }
}
