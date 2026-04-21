using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Task_Management_BE.Infrastructure;

namespace Task_Management_BE.Endpoints;

public class Health : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapHealthChecks("", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        status = report.Status.ToString(),
                        totalDuration = report.TotalDuration,
                        checks = report.Entries.ToDictionary(
                            entry => entry.Key,
                            entry => new
                            {
                                status = entry.Value.Status.ToString(),
                                description = entry.Value.Description,
                                duration = entry.Value.Duration,
                                error = entry.Value.Exception?.Message
                            })
                    };

                    await JsonSerializer.SerializeAsync(context.Response.Body, response, cancellationToken: context.RequestAborted);
                }
            });
    }
}
