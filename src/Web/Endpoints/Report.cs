using Application.Common.Interfaces;
using Application.Features.Reports.command.TasksReport;
using Application.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Task_Management_BE.Infrastructure;

namespace Task_Management_BE.Endpoints;

public class Report : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapPost(GenerateTaskReport);
        
    }

    public async Task<IResult> GenerateTaskReport(ISender sender, TasksReportCommand command, ICurrentUserService currentUserService)
    {
        command.UserId = currentUserService.UserId!;
        var id = await sender.Send(command);
        return TypedResults.Ok(Result<Guid>.SuccessResponse(200, "Generating report", id));
    }
}
