using Application.Common.Interfaces;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using StackExchange.Redis;


namespace Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Connection string 'TodoDb' not found.");
        
        QuestPDF.Settings.License = LicenseType.Community;
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
        
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ApplicationDbContextInitializer>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthResultHandler>();
        // services.AddAuthentication()
        //     .AddBearerToken(IdentityConstants.BearerScheme);
        // services.AddOptions<BearerTokenOptions>(IdentityConstants.BearerScheme).Configure(opt =>
        // {
        //     opt.BearerTokenExpiration = TimeSpan.FromMinutes(2);
        // });
        services.AddAuthentication(options =>
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
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey =
                        new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
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
        services.AddScoped<ITokenService, TokenService>();
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddApiEndpoints();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, AppUser>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator))
        );
        services.AddScoped<UserManager<ApplicationUser>, ApplicationUserManager>();
        services.AddTransient<IIdentityService, IdentityService>();
        services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });
        services.AddScoped<INotificationPublisherService, NotificationHubServices>();
        services.AddScoped<IReportService, ReportService>();
        services.AddHostedService<ReportBackgroundWorker>();
        services.AddSingleton<Cloudinary>((sp) =>
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
        services.AddSingleton<IBackgroundJobSignal, BackgroundJobSignal>();
        services.AddSingleton<IMessageBus>((sp) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            var hostName = config.GetValue<string>("RabbitMq:HostName") ?? "localhost";
            var userName = config.GetValue<string>("RabbitMq:UserName") ?? "admin";
            var password = config.GetValue<string>("RabbitMq:Password") ?? "admin";
            var virtualHost = config.GetValue<string>("RabbitMq:VirtualHost") ?? "/";
            var port = config.GetValue<int?>("RabbitMq:Port") ?? 5672;
            return new RabbitMqMessageBus(hostName, userName, password, virtualHost, port);
        });
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var redisConnectionString = 
                config.GetConnectionString("Caching:Redis:ConnectionString") ?? "localhost:6379";
            return ConnectionMultiplexer.Connect(redisConnectionString);
        });
        services.AddScoped<IRedisCacheService, RedisCacheService>();
    }
}
