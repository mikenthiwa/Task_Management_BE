using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Application.Common.Interfaces;

public interface ITokenService
{
    Task<(string tokenType, string token, string refreshToken, double expiresInMinutes)> GenerateTokensAsync(
        IdentityUser user);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    Task<(string tokenType, string token, string refreshToken, double expiresInMinutes)> RefreshTokensAsync(string refreshToken);
}
