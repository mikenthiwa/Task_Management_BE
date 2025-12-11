using System.Security.Claims;
using Application.Common.Interfaces;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Identity;

public class AppUser(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;
    public string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new NotFoundException("", "User ID not found in claims.");
    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;
    public string? Username => User?.FindFirst(ClaimTypes.Name)?.Value;
}
