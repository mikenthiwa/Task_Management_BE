namespace Application.Features.Auth.Command.Oauth;

public class TokenSetDto
{
    public required string TokenType { get; init; }
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required double ExpiresIn { get; init; }
}
