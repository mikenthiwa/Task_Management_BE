using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces;
using Ardalis.GuardClauses;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Infrastructure.Token;

public class TokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext): ITokenService
{
    private const string RefreshTokenProvider = "TaskManagement";
    private const string RefreshTokenName = "RefreshToken";
    
    public async Task<(string tokenType, string token, string refreshToken, double expiresInMinutes)> GenerateTokensAsync(
        IdentityUser user)
    {
        // Implementation for generating tokens
        var authClaims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var applicationUser = user as ApplicationUser ?? await userManager.FindByIdAsync(user.Id)
            ?? throw new NotFoundException(nameof(ApplicationUser), user.Id);
        var userRoles = await userManager.GetRolesAsync(applicationUser);
        authClaims.AddRange(userRoles.Select(r => new Claim("roles", r)));
        var jwtKey = configuration["Jwt:Key"];
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Guard.Against.NullOrWhiteSpace(jwtKey)));
        var expiresInMinutes = Convert.ToDouble(configuration["Jwt:AccessTokenExpiryMinutes"]);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );
        
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hashedRefreshToken = HashRefreshToken(refreshToken);
        await userManager.SetAuthenticationTokenAsync(applicationUser, loginProvider: RefreshTokenProvider, tokenName: RefreshTokenName, tokenValue: hashedRefreshToken);
        
        return ("Bearer", accessToken, refreshToken, expiresInMinutes);
    }

    public async Task<(string tokenType, string token, string refreshToken, double expiresInMinutes)> RefreshTokensAsync(string refreshToken)
    {
        
        var hashedRefreshToken = HashRefreshToken(Guard.Against.NullOrWhiteSpace(refreshToken));

        var storedToken = await dbContext.Set<IdentityUserToken<string>>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.LoginProvider == RefreshTokenProvider &&
                t.Name == RefreshTokenName &&
                t.Value == hashedRefreshToken);
                // t.Value == refreshToken);

        if (storedToken is null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var user = await userManager.FindByIdAsync(storedToken.UserId)
                   ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        return await GenerateTokensAsync(user);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var jwtKey = configuration["Jwt:Key"];
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Guard.Against.NullOrWhiteSpace(jwtKey))),
            ValidateLifetime = false
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        return principal;
    }
    
    private static string HashRefreshToken(string refreshToken)
    {
        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var firstPass = SHA256.HashData(bytes);
        var secondPass = SHA256.HashData(firstPass);
        return Convert.ToBase64String(secondPass);
    }
}
