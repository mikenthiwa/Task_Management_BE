using Application.Common.Interfaces;
using Application.Models;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
{
    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user?.UserName;
    }

    public async Task<Result> CreateUserAsync(string username, string email, string password)
    {
        var user = new ApplicationUser() { UserName = username, Email = email, };
        var result = await userManager.CreateAsync(user, password);
        return result.ToApplicationResult();
    }
}
