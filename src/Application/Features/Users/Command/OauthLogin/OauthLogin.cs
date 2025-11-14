using System.Runtime.InteropServices.JavaScript;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.Users.Command.OauthLogin;

public record OauthLoginCommand : IRequest<TokenSetDto>
{
    public required string Email { get; init; }
}

public class TokenSetDto
{
    public required string TokenType { get; init; }
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime ExpiresIn { get; init; }
}

public class OauthLogin(IIdentityService identityService, IApplicationDbContext applicationDbContext, ITokenService tokenService) : IRequestHandler<OauthLoginCommand, TokenSetDto>
{
    public async Task<TokenSetDto> Handle(OauthLoginCommand request, CancellationToken cancellationToken)
    {
        var googleUser = new GoogleUserDto { Username = "MichaelNthiwa", Email = "mike.nthiwa@gmail.com" };
        var existingUser = await identityService.GetUserByEmailAsync(googleUser.Email);
        if (existingUser == null)
        {
            var createdUser = await identityService.CreateGoogleUserAsync(googleUser);
            existingUser = createdUser;
            applicationDbContext.DomainUsers.Add(new DomainUser(existingUser.Id, googleUser.Username, googleUser.Email));
            await applicationDbContext.SaveChangesAsync(cancellationToken);
        }
        var (tokenType, token, refreshToken, expiresIn) = await tokenService.GenerateTokensAsync(existingUser);
        
        return new TokenSetDto
        {
            TokenType = tokenType,
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn
        };
    }
}
