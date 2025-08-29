using Application.Models;

namespace Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Result> CreateUserAsync(string userName, string email, string password);
}
