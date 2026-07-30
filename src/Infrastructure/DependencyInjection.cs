using Application.Common.Interfaces;
using Application.Common.Options;
using Ardalis.GuardClauses;
using CloudinaryDotNet;
using Domain.Constants;
using Infrastructure.BackgroundWorker;
using Infrastructure.Data;
using Infrastructure.Data.Interceptors;
using Infrastructure.Hubs;
using Infrastructure.Identity;
using Infrastructure.JobSignal;
using Infrastructure.Notifications;
using Infrastructure.RabbitMq;
using Infrastructure.Redis;
using Infrastructure.Reports;
using Infrastructure.Security;
using Infrastructure.Token; 
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using StackExchange.Redis;


namespace Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Connection string not found.");
        var notificationDispatchMode = GetNotificationDispatchMode(builder.Configuration);
        
        QuestPDF.Settings.License = LicenseType.Community;
        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
        
        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options
                .UseNpgsql(connectionString)
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });
        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        builder.Services.AddScoped<ApplicationDbContextInitializer>();
        builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthResultHandler>();
        // builder.Services.AddAuthentication()
        //     .AddBearerToken(IdentityConstants.BearerScheme);
        // builder.Services.AddOptions<BearerTokenOptions>(IdentityConstants.BearerScheme).Configure(opt =>
        // {
        //     opt.BearerTokenExpiration = TimeSpan.FromMinutes(2);
        // });
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey =
                        new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &
                            (path.StartsWithSegments("/notificationHub")))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            }
        );
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddApiEndpoints();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserService, AppUser>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator))
        );
        builder.Services.AddScoped<UserManager<ApplicationUser>, ApplicationUserManager>();
        builder.Services.AddTransient<IIdentityService, IdentityService>();
        builder.Services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });
        builder.Services.AddSingleton<IUserIdProvider, NotificationUserIdProvider>();
        builder.Services.AddScoped<INotificationPublisherService, NotificationHubServices>();
        builder.Services.AddScoped<IReportService, ReportService>();
        builder.Services.AddHostedService<ReportBackgroundWorker>();
        builder.Services.AddSingleton<Cloudinary>((sp) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var cloudinarySection = config.GetSection("Cloudinary");

            Account account = new Account(
                cloudinarySection["CloudName"],
                cloudinarySection["ApiKey"],
                cloudinarySection["ApiSecret"]);
            Cloudinary cloudinary = new Cloudinary(account);
            cloudinary.Api.Secure = true;
            return cloudinary;
        });
        builder.Services.AddSingleton<IBackgroundJobSignal, BackgroundJobSignal>();
        if (notificationDispatchMode.Equals(NotificationDispatchModes.RabbitMq, StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddSingleton<IMessageBus>((sp) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();

                var hostName = config.GetValue<string>("RabbitMq:HostName") ?? "localhost";
                var userName = config.GetValue<string>("RabbitMq:UserName") ?? "admin";
                var password = config.GetValue<string>("RabbitMq:Password") ?? "admin";
                var virtualHost = config.GetValue<string>("RabbitMq:VirtualHost") ?? "/";
                var port = config.GetValue<int?>("RabbitMq:Port") ?? 5672;
                var useSsl = config.GetValue<bool>("RabbitMq:UseSsl");
                return new RabbitMqMessageBus(hostName, userName, password, virtualHost, port, useSsl);
            });
            builder.Services.AddScoped<INotificationDispatcher, RabbitMqNotificationDispatcher>();
        }
        else
        {
            builder.Services.AddScoped<INotificationDispatcher, InProcessNotificationDispatcher>();
        }

        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var redisConnectionString = Guard.Against.NullOrWhiteSpace(
                config["Caching:Redis:ConnectionString"],
                "Redis connection string is not configured.");
            var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
            if (config.GetValue<bool>("Caching:Redis:SkipCertificateValidation"))
            {
                redisOptions.CertificateValidation += (_, _, _, _) => true;
            }

            return ConnectionMultiplexer.Connect(redisOptions);
        });
        builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

        return builder;
    }

    private static string GetNotificationDispatchMode(IConfiguration configuration)
    {
        var configuredMode = configuration[$"{NotificationDispatchOptions.SectionName}:DispatchMode"];
        if (!string.IsNullOrWhiteSpace(configuredMode))
        {
            if (configuredMode.Equals(NotificationDispatchModes.InProcess, StringComparison.OrdinalIgnoreCase)
                || configuredMode.Equals(NotificationDispatchModes.RabbitMq, StringComparison.OrdinalIgnoreCase))
            {
                return configuredMode;
            }

            throw new InvalidOperationException(
                $"Unsupported notification dispatch mode '{configuredMode}'. Supported values are '{NotificationDispatchModes.InProcess}' and '{NotificationDispatchModes.RabbitMq}'.");
        }

        var environment = configuration["ASPNETCORE_ENVIRONMENT"];
        return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase)
            ? NotificationDispatchModes.RabbitMq
            : NotificationDispatchModes.InProcess;
    }
}
