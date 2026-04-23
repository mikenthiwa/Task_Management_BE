using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Task_Management_BE.Infrastructure;

namespace Task_Management_BE.Endpoints;

public class Health : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapHealthChecks("", CreateHealthCheckOptions("ready"));
        group.MapHealthChecks("/live", CreateHealthCheckOptions("live"));
    }

    private static HealthCheckOptions CreateHealthCheckOptions(string tag)
    {
        return new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(tag),
            ResponseWriter = WriteResponseAsync
        };
    }

    private static Task WriteResponseAsync(HttpContext context, HealthReport report)
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

        return JsonSerializer.SerializeAsync(context.Response.Body, response, cancellationToken: context.RequestAborted);
    }
}
