using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.Reports.command.TasksReport;

public record TasksReportCommand : IRequest<byte[]>
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
}
public class TasksReport(IReportService reportService) : IRequestHandler<TasksReportCommand, byte[]>
{
    public async Task<byte[]> Handle(TasksReportCommand request, CancellationToken cancellationToken)
    {
        var report = await reportService.GenerateTasksReportAsync(request);
        return report;
    }
}
