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
using Npgsql;
using Task = System.Threading.Tasks.Task;

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
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration
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
        
        var user = await userManager.FindByNameAsync("michaelnthiwa");
        if (user is null)
        {
            user = new ApplicationUser() { UserName = "michaelnthiwa", Email = "mikenthiwa@gmail.com", Picture = "https://lh3.googleusercontent.com/a/ACg8ocJX24gftNNPXzvDtWVUN-DTI3aImb7CkOsSHuQiwtWKnnhPlx5rew=s96-c"};
            await userManager.CreateAsync(user);
            if (!string.IsNullOrWhiteSpace(userRole.Name))
            {
                await userManager.AddToRolesAsync(user, new [] { userRole.Name });
            }
        }

        if (!dbContext.DomainUsers.Any(u => u.Id == user.Id))
        {
            dbContext.DomainUsers.Add(new DomainUser(user.Id, user.UserName!, user.Email!, user.Picture));
        }

        await dbContext.SaveChangesAsync();

        if (!isDevelopment)
        {
            return;
        }

        var taskSeedCount = configuration.GetValue("SeedData:DevelopmentTaskCount", 500_000);
        if (taskSeedCount <= 0)
        {
            return;
        }
        
        // await dbContext.Database.ExecuteSqlRawAsync(
        //     """
        //     INSERT INTO "Tasks" ("Id", "Title", "Description", "Status", "Priority", "CreatorId", "AssigneeId", "CreatedAt")
        //     SELECT
        //         ('00000000-0000-0000-0000-' || lpad(i::text, 12, '0'))::uuid,
        //         'Task ' || i::text,
        //         'This is task number ' || i::text,
        //         0,
        //         0,
        //         {1},
        //         {1},
        //         now() - (i * interval '1 minute')
        //     FROM generate_series(1, {0}) AS i
        //     """,
        //     taskSeedCount,
        //     user.Id
        // );
        
        var connectionString = dbContext.Database.GetConnectionString() ?? throw new InvalidOperationException("Connection string not found.");
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using var writer = await conn.BeginBinaryImportAsync(
            """
            COPY "Tasks" ("Id","Title","Description","Status","Priority","CreatorId","AssigneeId","CreatedAt")
            FROM STDIN (FORMAT BINARY)
            """
        );
        for (int i = 1; i <= 500_000; i++)
        {
            writer.StartRow();
            writer.Write(Guid.NewGuid());
            writer.Write($"Task {i}");
            writer.Write($"This is task number {i}");
            writer.Write(0);
            writer.Write(0);
            writer.Write(user.Id);
            writer.Write(user.Id);
            writer.Write(DateTime.UtcNow.AddMinutes(-i));
        }
        await writer.CompleteAsync();
    }
    
}

