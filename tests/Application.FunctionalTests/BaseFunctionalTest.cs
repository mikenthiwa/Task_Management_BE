using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Application.FunctionalTests;

public class BaseFunctionalTest : IClassFixture<CustomWebApplicationFactory>
{
    protected CustomWebApplicationFactory Factory { get; }
    protected HttpClient HttpClient { get; }

    protected BaseFunctionalTest(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        HttpClient = factory.CreateClient();
        EnsureUserAsync(
                "test-user-id",
                "test-user",
                "test-user@example.com",
                "https://example.com/test-user.png")
            .GetAwaiter()
            .GetResult();
    }

    protected async System.Threading.Tasks.Task EnsureUserAsync(string userId, string userName, string email, string picture)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!dbContext.Users.Any(u => u.Id == userId))
        {
            var appUser = new ApplicationUser
            {
                Id = userId,
                UserName = userName,
                Email = email,
                Picture = picture,
                NormalizedUserName = userName.ToUpperInvariant(),
                NormalizedEmail = email.ToUpperInvariant()
            };
            await userManager.CreateAsync(appUser);
        }

        if (!dbContext.DomainUsers.Any(u => u.Id == userId))
        {
            dbContext.DomainUsers.Add(new DomainUser(userId, userName, email, picture));
            await dbContext.SaveChangesAsync();
        }
    }
}
