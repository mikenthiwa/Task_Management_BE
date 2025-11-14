using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Application.Common.Interfaces;

public interface ITokenService
{
    Task<(string tokenType, string token, string refreshToken, DateTime expiresIn)> GenerateTokensAsync(
        IdentityUser user);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
