using Application.Common.Interfaces;
using Application.Features.Reports.command.TasksReport;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Domain.Enum;
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
        var cloudinary = scope.ServiceProvider.GetRequiredService<Cloudinary>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

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
                
                var stream = new MemoryStream(fileBytes);
                var uploadParams = new RawUploadParams()
                {
                    File = new FileDescription($"{job.Id}.pdf", stream),
                    Folder = "reports",
                    PublicId = job.Id.ToString(),

                    Overwrite = true,
                    UseFilename = true,
                    UniqueFilename = false
                };
                var uploadResult = await cloudinary.UploadAsync(uploadParams);
                var downloadUrl = uploadResult.SecureUrl.ToString().Replace("/upload/", "/upload/fl_attachment/");

                job.Status = "Completed";
                job.FilePath = downloadUrl;
                job.CompletedAt = DateTime.UtcNow;
                var message = $"Your report is ready to download";
                await notificationService.CreateNotificationAsync(
                    job.RequestedByUserId,
                    message,
                    NotificationType.TasksReportGenerated,
                    downloadUrl,
                    "Download Report"
                    );
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
