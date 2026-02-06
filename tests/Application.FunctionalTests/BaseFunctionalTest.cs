using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Application.FunctionalTests;

public class BaseFunctionalTest : IClassFixture<CustomWebApplicationFactory>
{
    protected HttpClient HttpClient { get; }

    protected BaseFunctionalTest(CustomWebApplicationFactory factory)
    {
        HttpClient = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        const string userId = "test-user-id";
        if (!dbContext.Users.Any(u => u.Id == userId))
        {
            var appUser = new ApplicationUser
            {
                Id = userId,
                UserName = "test-user",
                Email = "test-user@example.com",
                Picture = "https://example.com/test-user.png",
                NormalizedUserName = "TEST-USER",
                NormalizedEmail = "TEST-USER@EXAMPLE.COM"
            };
            userManager.CreateAsync(appUser).GetAwaiter().GetResult();
        }

        if (!dbContext.DomainUsers.Any(u => u.Id == userId))
        {
            dbContext.DomainUsers.Add(new DomainUser(
                userId,
                "test-user",
                "test-user@example.com",
                "https://example.com/test-user.png"));
            dbContext.SaveChanges();
        }
    }
}
