namespace Application.Features.Users.Command.Oauth;

public class TokenSetDto
{
    public required string TokenType { get; init; }
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime ExpiresIn { get; init; }
}
