using Application.Common.Interfaces;
using Application.Features.Reports.command.TasksReport;
using Application.Features.Tasks.Queries.GetTasksWithPagination;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace Infrastructure.Reports;

public class ReportService(IApplicationDbContext applicationDbContext, IMapper mapper) : IReportService
{
    public async Task<byte[]> GenerateTasksReportAsync(TasksReportCommand request)
    {
        var data = await applicationDbContext.Tasks
            .Where(task => task.CreatedAt >= request.From && task.CreatedAt <= request.To)
            .AsNoTracking()
            .ProjectTo<TaskDto>(mapper.ConfigurationProvider)
            .ToListAsync();
        return GeneratePdf(data);
    } 

    private byte[] GeneratePdf(List<TaskDto> data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text("Tasks Report").Bold().FontSize(18);
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });
                    table.Header(header =>
                    {
                        header.Cell().Text("Title");
                        header.Cell().Text("Description");
                    });
                    foreach (var row in data)
                    {
                        table.Cell().Text(row.Id.ToString());
                        table.Cell().Text(row.Description);
                    }
                });

            });
        });
        return document.GeneratePdf();
    }
}
