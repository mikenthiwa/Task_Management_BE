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
    
    public async Task<IdentityUser> CreateGoogleUserAsync(GoogleUserDto googleUser)
    {
        var user = new ApplicationUser() { Email = googleUser.Email, UserName = googleUser.Username, Picture =  googleUser.Picture };
        await userManager.CreateAsync(user);
        return user;
    }
    
    public async Task<IdentityUser?> GetUserByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user;
    }
    
}
