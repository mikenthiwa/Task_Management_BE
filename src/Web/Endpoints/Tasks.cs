using Application.Features.Tasks.Command.CreateTask;
using Application.Models;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using Task_Management_BE.Infrastructure;

namespace Task_Management_BE.Endpoints;

public class Tasks : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization(Policies.CanPurge)
            .AddFluentValidationAutoValidation()
            .MapPost("/", CreateTask);
    }
    
    private async Task<Results<Ok<Result>, BadRequest>> CreateTask(ISender sender, CreateTaskCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Ok(Result.SuccessResponse(201, "Task created successfully"));
    }
}
