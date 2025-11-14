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
        var result = await userManager.CreateAsync(user);
        return result.ToApplicationResult();
    }
    

    public async Task<IdentityUser> CreateGoogleUserAsync(GoogleUserDto googleUser)
    {
        var user = new ApplicationUser() { Email = googleUser.Email, UserName = googleUser.Username };
        await userManager.CreateAsync(user);
        return user;
    }
    
    public async Task<IdentityUser?> GetUserByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user;
    }
    
}
