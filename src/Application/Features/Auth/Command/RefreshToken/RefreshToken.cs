using Application.Common.Interfaces;
using Application.Features.Auth.Command.Oauth;
using MediatR;

namespace Application.Features.Auth.Command.RefreshToken;

public record RefreshTokenCommand: IRequest<TokenSetDto>
{
    public required string RefreshToken { get; set; }
}

public class RefreshToken(ITokenService tokenService) : IRequestHandler<RefreshTokenCommand, TokenSetDto>
{
    public async Task<TokenSetDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var (tokenType, accessToken, refreshToken, expiresIn) =
            await tokenService.RefreshTokensAsync(request.RefreshToken);

        return new TokenSetDto
        {
            TokenType = tokenType,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn
        };
    }
}
