using Application.Common.Interfaces;
using Application.Models;
using Domain.Entities;
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
    

    public async Task<IdentityUser> CreateGoogleUserAsync(GoogleUserDto googleUser)
    {
        var user = new ApplicationUser() { Email = googleUser.Email, UserName = googleUser.Username };
        var result = await userManager.CreateAsync(user);
        if(!result.Succeeded) throw new Exception("Failed to create Google user.");
        return user;
    }
    
    public async Task<IdentityUser?> GetUserByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user;
    }
    
}
