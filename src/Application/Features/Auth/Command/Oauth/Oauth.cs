using MediatR;
using Application.Common.Interfaces;

namespace Application.Features.Auth.Command.Oauth;

public record OauthCommand : IRequest<TokenSetDto>
{
    public required string Email { get; init; }
    public required string Username { get; init; }
    public required string Picture { get; init; }
}
public class Oauth(IIdentityService identityService, ITokenService tokenService, IApplicationDbContext applicationDbContext) : IRequestHandler<OauthCommand, TokenSetDto>
{
    public async Task<TokenSetDto> Handle(OauthCommand request, CancellationToken cancellationToken)
    {
        var googleUser = new GoogleUserDto { Username = request.Username, Email = request.Email, Picture =  request.Picture };
        var existingUser = await identityService.GetUserByEmailAsync(googleUser.Email);
        if (existingUser == null)
        {
            var createdUser = await identityService.CreateGoogleUserAsync(googleUser);
            existingUser = createdUser;
        }

        var user = await applicationDbContext.DomainUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == existingUser.Id, cancellationToken);

        var (tokenType, token, refreshToken, expiresInMinutes) = await tokenService.GenerateTokensAsync(user!);
        
        return new TokenSetDto
        {
            TokenType = tokenType,
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresIn = expiresInMinutes
        };
    }
}
