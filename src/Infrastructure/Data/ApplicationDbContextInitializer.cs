using Domain.Constants;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;
using TaskItem = Domain.Entities.Task;

namespace Infrastructure.Data;

public static class InitializerExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication application)
    {
        using var scope = application.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var shouldSeed = environment.IsDevelopment() || configuration.GetValue<bool?>("SeedData:Enabled") == true;

        await initializer.InitializeAsync(environment.IsDevelopment());
        await initializer.SeedAsync(shouldSeed, environment.IsDevelopment());
    }
}

public class ApplicationDbContextInitializer(
    ILogger<ApplicationDbContextInitializer> logger,
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager
    )
{
    
    public async Task InitializeAsync(bool isDevelopment)
    {
        try
        {
            if (isDevelopment)
            {
                // See https://jasontaylor.dev/ef-core-database-initialisation-strategies
                await dbContext.Database.EnsureDeletedAsync();
                await dbContext.Database.EnsureCreatedAsync();
            }
            else
            {
                Console.WriteLine("This line should be running in PROD!");
                await dbContext.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }

    public async Task SeedAsync(bool shouldSeed, bool isDevelopment)
    {
        try
        {
            if (!shouldSeed)
            {
                return;
            }
            
            await TrySeedAsync(isDevelopment);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task TrySeedAsync(bool isDevelopment)
    {
        var userRole = new IdentityRole(Roles.User);
        if (roleManager.Roles.All(r => r.Name != userRole.Name))
        {
            await roleManager.CreateAsync(userRole);
        }
        
        var user = new ApplicationUser() { UserName = "michaelnthiwa", Email = "mikenthiwa@gmail.com", Picture = "https://lh3.googleusercontent.com/a/ACg8ocJX24gftNNPXzvDtWVUN-DTI3aImb7CkOsSHuQiwtWKnnhPlx5rew=s96-c"};
        if (userManager.Users.All(u => u.UserName != user.UserName))
        {
            await userManager.CreateAsync(user);
            if (!string.IsNullOrWhiteSpace(userRole.Name))
            {
                await userManager.AddToRolesAsync(user, new [] { userRole.Name });
            }

            if (!dbContext.DomainUsers.Any(u => u.Id == user.Id))
            {
                dbContext.DomainUsers.Add(new DomainUser(user.Id, user.UserName, user.Email, "https://lh3.googleusercontent.com/a/ACg8ocJX24gftNNPXzvDtWVUN-DTI3aImb7CkOsSHuQiwtWKnnhPlx5rew=s96-c"));
            }
        }
        
        
        if (!dbContext.Tasks.Any())
        {
            for (int i = 1; i <= 15; i++)
            {
                var task = new TaskItem
                {
                    Title = $"Task {i}",
                    Description = $"This is task number {i}.",
                    Status = 0,
                    Priority = 0,
                    CreatorId = user.Id,
                    AssigneeId = user.Id
                };
                dbContext.Tasks.Add(task);
            }
        }
        
        await dbContext.SaveChangesAsync();
    }
    
}
