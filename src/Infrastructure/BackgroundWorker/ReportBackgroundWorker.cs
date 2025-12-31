using Application.Common.Interfaces;
using Application.Features.Reports.command.TasksReport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundWorker;

public class ReportBackgroundWorker(IServiceScopeFactory scopeFactory, ILogger<ReportBackgroundWorker> logger) : BackgroundService
{
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Report background worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingReports(stoppingToken);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error processing report jobs");

            }
            
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
    
    private async Task ProcessPendingReports(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        var notificationPublisherService = scope.ServiceProvider.GetRequiredService<INotificationPublisherService>();

        var pendingJobs = await db.ReportJobs
            .Where(j => j.Status == "Pending")
            .OrderBy(j => j.CreatedAt)
            .Take(5)
            .ToListAsync(ct);

        foreach (var job in pendingJobs)
        {
            try
            {
                job.Status = "Processing";
                await db.SaveChangesAsync(ct);

                var fileBytes = await reportService.GenerateTasksReportAsync(
                    new TasksReportCommand
                    {
                        From = job.From,
                        To = job.To
                    }
                );
                var userId = job.RequestedByUserId;

                // Save file locally or to blob storage
                var filePath = $"Reports/{job.Id}.pdf";
                await File.WriteAllBytesAsync(filePath, fileBytes, ct);

                job.Status = "Completed";
                job.FilePath = filePath;
                job.CompletedAt = DateTime.UtcNow;
                await notificationPublisherService.NotifyReportGeneratedAsync(userId, filePath);

            }
            catch (Exception ex)
            {
                job.Status = "Failed";
                job.ErrorMessage = ex.Message;
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
