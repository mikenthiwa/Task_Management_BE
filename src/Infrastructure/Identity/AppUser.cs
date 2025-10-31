using System.Security.Claims;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Identity;

public class AppUser(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;
    public string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;
    public string? Username => User?.FindFirst(ClaimTypes.Name)?.Value;
}
