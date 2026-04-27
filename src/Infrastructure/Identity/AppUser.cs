using System.Security.Claims;
using Application.Common.Interfaces;
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Http;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Infrastructure.Identity;

public class AppUser(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;
    public string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                             ?? throw new NotFoundException("", "User ID not found in claims.");
    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value
                            ?? User?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
    public string? Username => User?.FindFirst(ClaimTypes.Name)?.Value;
}
