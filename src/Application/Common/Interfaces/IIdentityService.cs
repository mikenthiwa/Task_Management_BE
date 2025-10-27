using Application.Models;

namespace Application.Common.Interfaces;

public class UserDto
{
    public string? Id { get; set; }
    public string? Email { get; set; }
    
}

public interface IIdentityService
{
    Task<Result> CreateUserAsync(string userName, string email, string password);
    Task<string?> GetUserNameAsync(string userId);
}
