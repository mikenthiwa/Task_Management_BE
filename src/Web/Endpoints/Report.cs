using Application.Features.Reports.command.TasksReport;
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

    public async Task<IResult> GenerateTaskReport(ISender sender, TasksReportCommand command)
    {
        var file = await sender.Send(command);
        var fileName = $"$tasks-report-{DateTime.UtcNow:MM/dd/yyyy}.pdf";
        return Results.File(file, "application/pdf", fileName);
    }
}
