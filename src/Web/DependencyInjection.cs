using System.Text.Json.Serialization;
using Application.Common.Options;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSwag;
using Task_Management_BE.HealthChecks;
using Web.Infrastructure;

namespace Task_Management_BE;

public static class DependencyInjection
{
    public static void AddWebServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = Guard.Against.NullOrWhiteSpace(
            configuration.GetConnectionString("DefaultConnection"),
            message: "Connection string 'DefaultConnection' is not configured.");
        var redisConnectionString = Guard.Against.NullOrWhiteSpace(
            configuration["Caching:Redis:ConnectionString"],
            message: "Redis connection string is not configured.");

        var allowedOrigins = configuration["Cors:AllowedOrigins"]
            ?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        services.AddOpenApi();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddEndpointsApiExplorer();
        services.AddProblemDetails();
        
        services.AddOpenApiDocument((configure, sp) =>
        {
            configure.Title = "Task Management API";
            configure.AddSecurity("JWT", Enumerable.Empty<string>(), new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.ApiKey,
                Name = "Authorization",
                In = OpenApiSecurityApiKeyLocation.Header,
                Description = "Type into the textbox: Bearer {your JWT token}."
            });
        });
        services.Configure<JsonOptions>(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())
        );
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter())
        );
        services.AddCors(options =>
        {
            options.AddPolicy("MyAllowSpecificOrigins", builder =>
            {
                var origins = allowedOrigins.Length > 0 ? allowedOrigins : ["http://localhost:3000"];
                builder.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
            
        });
        var healthChecks = services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"])
            .AddNpgSql(connectionString, name: "postgresql", tags: ["ready"])
            .AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);

        if (IsRabbitMqNotificationDispatchEnabled(configuration))
        {
            healthChecks.AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);
        }
    }

    private static bool IsRabbitMqNotificationDispatchEnabled(IConfiguration configuration)
    {
        var configuredMode = configuration[$"{NotificationDispatchOptions.SectionName}:DispatchMode"];
        if (!string.IsNullOrWhiteSpace(configuredMode))
        {
            if (configuredMode.Equals(NotificationDispatchModes.RabbitMq, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (configuredMode.Equals(NotificationDispatchModes.InProcess, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            throw new InvalidOperationException(
                $"Unsupported notification dispatch mode '{configuredMode}'. Supported values are '{NotificationDispatchModes.InProcess}' and '{NotificationDispatchModes.RabbitMq}'.");
        }

        return string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase);
    }
}
