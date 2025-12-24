using Application.Models;
using Microsoft.AspNetCore.Identity;

namespace Application.Common.Interfaces;

public class GoogleUserDto
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Picture { get; set; }
}

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);
    
    public Task<IdentityUser> CreateGoogleUserAsync(GoogleUserDto googleUser);
    
    public Task<IdentityUser?> GetUserByEmailAsync(string email);
    
    
}
