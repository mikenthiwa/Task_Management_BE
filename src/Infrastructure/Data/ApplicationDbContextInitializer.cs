using Domain.Constants;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;
using TaskItem = Domain.Entities.Task;

namespace Infrastructure.Data;

public static class InitializerExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication application)
    {
        using var scope = application.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();
        
        await initializer.InitializeAsync();
        await initializer.SeedAsync();
    }
}

public class ApplicationDbContextInitializer(ILogger<ApplicationDbContextInitializer> logger, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
{
    
    public async Task InitializeAsync()
    {
        try
        {
            // See https://jasontaylor.dev/ef-core-database-initialisation-strategies
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task TrySeedAsync()
    {
        var administratorRole = new IdentityRole(Roles.Administrator);
        if (roleManager.Roles.All(r => r.Name != administratorRole.Name))
        {
            await roleManager.CreateAsync(administratorRole);
        }
        
        var administrator = new ApplicationUser() { UserName = "administrator", Email = "administrator@localhost", EmailConfirmed = true};
        if (userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            await userManager.CreateAsync(administrator, "Kenya2019%");
            if (!string.IsNullOrWhiteSpace(administratorRole.Name))
            {
                await userManager.AddToRolesAsync(administrator, new [] { administratorRole.Name });
            }

            if (!dbContext.DomainUsers.Any(u => u.Id == administrator.Id))
            {
                dbContext.DomainUsers.Add(new DomainUser(administrator.Id, administrator.UserName, administrator.Email));
            }
            
        }
        
        var userRole = new IdentityRole(Roles.User);
        if (roleManager.Roles.All(r => r.Name != userRole.Name))
        {
            await roleManager.CreateAsync(userRole);
        }
        
        var user = new ApplicationUser() { UserName = "user", Email = "user@localhost" };
        if (userManager.Users.All(u => u.UserName != user.UserName))
        {
            await userManager.CreateAsync(user, "User1!");
            if (!string.IsNullOrWhiteSpace(userRole.Name))
            {
                await userManager.AddToRolesAsync(user, new [] { userRole.Name });
            }

            if (!dbContext.DomainUsers.Any(u => u.Id == user.Id))
            {
                dbContext.DomainUsers.Add(new DomainUser(user.Id, user.UserName, administrator.Email));
            }
        }
        
        
        // Seed, Tasks
        if (!dbContext.Tasks.Any())
        {
            for (int i = 1; i <= 15; i++)
            {
                dbContext.Tasks.Add(
                    new TaskItem
                    {
                        Title = $"Task {i}",
                        Description = $"This is task number {i}.",
                        Status = 0,
                        Priority = 0,
                        CreatorId = administrator.Id,
                        AssigneeId = user.Id
                    }
                );
            }
        }
        await dbContext.SaveChangesAsync();
    }
    
}
