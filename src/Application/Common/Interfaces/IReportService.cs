using Application.Features.Reports;
using Application.Features.Reports.command.TasksReport;

namespace Application.Common.Interfaces;

public interface IReportService
{
    Task<byte[]> GenerateTasksReportAsync(TasksReportCommand request);
}
