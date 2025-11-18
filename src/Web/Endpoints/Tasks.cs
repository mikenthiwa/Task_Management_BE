using Application.Common.Models;
using Application.Features.Tasks.Command.AssignTask;
using Application.Features.Tasks.Command.CreateTask;
using Application.Features.Tasks.Command.Queries.GetTasksWithPagination;
using Application.Models;
using Domain.Constants;
using Domain.Enum;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using Task_Management_BE.Infrastructure;

namespace Task_Management_BE.Endpoints;

public class Tasks : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            // .RequireAuthorization(Policies.CanPurge)
            .RequireAuthorization()
            .AddFluentValidationAutoValidation()
            .MapPost("/", CreateTask);

        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet("/", GetTasks);

        app.MapGroup(this)
            // .RequireAuthorization(Policies.CanPurge)
            .RequireAuthorization()
            .MapPost("/{taskId:int}/assign", AssignTask);
    }
    
    private async Task<Results<Ok<Result>, BadRequest>> CreateTask(ISender sender, CreateTaskCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Ok(Result.SuccessResponse(201, "Task created successfully"));
    }

    private async Task<Results<Ok<Result<PaginatedList<TaskDto>>>, BadRequest>> GetTasks(
        ISender sender,
        [FromQuery(Name = "status")] Status? status,
        [FromQuery(Name = "AssigneeId")] string? assignedId,
        [FromQuery(Name = "PageNumber")] int? pageNumber,
        [FromQuery(Name = "PageSize")] int? pageSize
        )
    {
        var query = new GetTaskWithQuery { Status = status, AssigneeId = assignedId, PageNumber = pageNumber ?? 1, PageSize = pageSize ?? 10 };
        var result = await sender.Send(query);
        return TypedResults.Ok(Result<PaginatedList<TaskDto>>.SuccessResponse(200, "Tasks retrieved successfully", result));
    }

    private async Task<Results<Ok<Result<TaskDto>>, BadRequest>> AssignTask(
        [FromRoute] int taskId,
        ISender sender,
        [FromBody] AssignTaskCommand command
    )
    {
        command.TaskId = taskId;
        var result = await sender.Send(command);
        return TypedResults.Ok(Result<TaskDto>.SuccessResponse(200, "Task assigned successfully", result));
    }
}
