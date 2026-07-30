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
    public static IHostApplicationBuilder AddWebServices(this IHostApplicationBuilder builder)
    {
        var connectionString = Guard.Against.NullOrWhiteSpace(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            message: "Connection string 'DefaultConnection' is not configured.");
        var redisConnectionString = Guard.Against.NullOrWhiteSpace(
            builder.Configuration["Caching:Redis:ConnectionString"],
            message: "Redis connection string is not configured.");
        
        builder.Services.AddOpenApi();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddProblemDetails();
        
        builder.Services.AddOpenApiDocument((configure, sp) =>
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
        builder.Services.Configure<JsonOptions>(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())
        );
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter())
        );
        var allowedOrigins = builder.Configuration.GetSection(CorsOptions.SectionName)
            .Get<CorsOptions>()?
            .AllowedOrigins.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        var isDevelopment = builder.Environment.IsDevelopment();
        if (!isDevelopment && allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException(
                "Cors:AllowedOrigins must be configured outside Development.");
        }

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(CorsOptions.PolicyName, policy =>
            {
                if (isDevelopment)
                {
                    var origins = allowedOrigins.Length > 0 ? allowedOrigins : ["http://localhost:3000"];
                    policy.WithOrigins(origins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();

                    return;
                }

                policy.WithOrigins(allowedOrigins)
                    .WithMethods("GET", "POST", "PATCH", "OPTIONS")
                    .WithHeaders("Content-Type", "Authorization")
                    .AllowCredentials();
            });
        });
        var healthChecks = builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"])
            .AddNpgSql(connectionString, name: "postgresql", tags: ["ready"])
            .AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);

        if (IsRabbitMqNotificationDispatchEnabled(builder.Configuration))
        {
            healthChecks.AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);
        }

        return builder;
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
