using System.Net;
using System.Net.Http.Json;
using Application.Features.Auth.Command.Oauth;
using Application.Models;
using FluentAssertions;

namespace Application.FunctionalTests.Auth.Command;

public class OauthTest(CustomWebApplicationFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task ShouldRequireEmail()
    {
        var command = new OauthCommand
        {
            Email = "",
            Username = "test-user",
            Picture = "https://example.com/test-user.png"
        };

        var response = await HttpClient.PostAsJsonAsync("/api/Auth/social-login", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldRequireUsername()
    {
        var command = new OauthCommand
        {
            Email = "test-user@example.com",
            Username = "",
            Picture = "https://example.com/test-user.png"
        };

        var response = await HttpClient.PostAsJsonAsync("/api/Auth/social-login", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldReturnTokensForValidUser()
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        var command = new OauthCommand
        {
            Email = $"oauth-{uniqueId}@example.com",
            Username = $"oauth-user-{uniqueId}",
            Picture = "https://example.com/oauth-user.png"
        };

        var response = await HttpClient.PostAsJsonAsync("/api/Auth/social-login", command);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<TokenSetDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Data.TokenType.Should().NotBeNullOrWhiteSpace();
        result.Data.ExpiresIn.Should().BeGreaterThan(0);
    }
}
