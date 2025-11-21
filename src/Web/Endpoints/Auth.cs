using Application.Features.Auth.Command.Oauth;
using Application.Features.Auth.Command.RefreshToken;
using Task_Management_BE.Infrastructure;
using Application.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;

namespace Task_Management_BE.Endpoints;

public class Auth : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        // app.MapGroup(this)
        //     .AddFluentValidationAutoValidation()
        //     .MapPost(Oauth, "/social-login")
        //     .MapPost(RefreshToken, "/refresh-token");
        
        app.MapGroup(this)
            .AddFluentValidationAutoValidation()
            .MapPost(Oauth, "/social-login")
            .MapPost(RefreshToken, "/refresh-token");
    }
    
    private async Task<Results<Ok<Result<TokenSetDto>>, BadRequest>> Oauth(ISender sender, OauthCommand command)
    {
        var result = await sender.Send(command);
        return TypedResults.Ok(Result<TokenSetDto>.SuccessResponse(200, "Login successful", result));
    }
    
    private async Task<Results<Ok<Result<TokenSetDto>>, BadRequest>> RefreshToken(ISender sender, RefreshTokenCommand command)
    {
        var result = await sender.Send(command);
        return TypedResults.Ok(Result<TokenSetDto>.SuccessResponse(200, "Token refreshed successfully", result));
    }
}
