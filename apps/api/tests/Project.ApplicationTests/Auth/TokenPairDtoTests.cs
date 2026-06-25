using Project.Application.Auth;

namespace Project.ApplicationTests.Auth;

public sealed class TokenPairDtoTests
{
    [Fact]
    public void Constructor_WithAllFields_SetsRefreshTokenForCookie()
    {
        // ARRANGE & ACT
        var dto = new TokenPairDto("jwt.token", 900, "raw-refresh-token");

        // ASSERT — Slice 3 AuthController needs the raw refresh token to set the HttpOnly cookie
        Assert.Equal("jwt.token", dto.AccessToken);
        Assert.Equal(900, dto.ExpiresInSeconds);
        Assert.Equal("raw-refresh-token", dto.RefreshToken);
    }
}
