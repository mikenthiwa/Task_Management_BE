using MediatR;
using Application.Common.Interfaces;
using Application.Features.Users.Command.Oauth;

namespace Application.Features.Users.Command.Oauth;

public record OauthCommand : IRequest<TokenSetDto>
{
    public required string Email { get; init; }
    public required string Username { get; init; }
}
public class Oauth(IIdentityService identityService, ITokenService tokenService) : IRequestHandler<OauthCommand, TokenSetDto>
{
    public async Task<TokenSetDto> Handle(OauthCommand request, CancellationToken cancellationToken)
    {
        var googleUser = new GoogleUserDto { Username = "MichaelNthiwa", Email = "mike.nthiwa@gmail.com" };
        var existingUser = await identityService.GetUserByEmailAsync(googleUser.Email);
        if (existingUser == null)
        {
            var createdUser = await identityService.CreateGoogleUserAsync(googleUser);
            existingUser = createdUser;
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
