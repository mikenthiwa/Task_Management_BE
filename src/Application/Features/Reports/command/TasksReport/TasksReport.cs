using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Reports.command.TasksReport;

public record TasksReportCommand : IRequest<Guid>
{
    public DateTimeOffset From { get; init; }
    public DateTimeOffset To { get; init; }
    public string UserId { get; set; } = string.Empty;
}
public class TasksReport(IApplicationDbContext applicationDbContext, IBackgroundJobSignal signal) : IRequestHandler<TasksReportCommand, Guid>
{
    public async Task<Guid> Handle(TasksReportCommand request, CancellationToken cancellationToken)
    {
        var job = new ReportJob { From = request.From, To = request.To, Status = "Pending", RequestedByUserId = request.UserId};
        applicationDbContext.ReportJobs.Add(job);
        await applicationDbContext.SaveChangesAsync(cancellationToken);
        signal.Signal();
        return  job.Id;
    }
}
