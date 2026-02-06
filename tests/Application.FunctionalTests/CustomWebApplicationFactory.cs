using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authorization;
using Testcontainers.PostgreSql;
using IMessageBus = Application.Common.Interfaces.IMessageBus;

namespace Application.FunctionalTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _databaseContainer = new PostgreSqlBuilder("postgres:15-alpine")
        .WithDatabase("testdb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.UseNpgsql(_databaseContainer.GetConnectionString());
            });
            
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "Test", options => { });
            services.AddAuthorization();
            services.PostConfigureAll<AuthorizationOptions>(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes("Test")
                    .RequireAuthenticatedUser()
                    .Build();
            });
            services.RemoveAll<IMessageBus>();
            services.TryAddSingleton<IMessageBus, NoOpMessageBus>();
        });
    }
    public Task InitializeAsync()
    {
        return _databaseContainer.StartAsync();
    }
    
    public new Task DisposeAsync()
    {
        return _databaseContainer.StopAsync();
    }
}
