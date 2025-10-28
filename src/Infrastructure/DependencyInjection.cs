using Application.Common.Interfaces;
using Domain.Constants;
using Infrastructure.Data;
using Infrastructure.Data.Interceptors;
using Infrastructure.Identity;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Connection string 'TodoDb' not found.");
        Console.WriteLine($"Database connection >>>>>>>>>>: {connectionString}");
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ApplicationDbContextInitializer>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthResultHandler>();
        services.AddAuthentication()
            .AddBearerToken(IdentityConstants.BearerScheme);
        services.AddOptions<BearerTokenOptions>(IdentityConstants.BearerScheme).Configure(opt =>
        {
            opt.BearerTokenExpiration = TimeSpan.FromMinutes(2);
        });

        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddApiEndpoints();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, AppUser>();
        services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator))
        );
        services.AddTransient<IIdentityService, IdentityService>();
        services.AddScoped<UserManager<ApplicationUser>, ApplicationUserManager>();
    }
}
