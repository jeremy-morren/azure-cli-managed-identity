using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Azure.Core;
using Microsoft.IdentityModel.Tokens;

namespace AzCliManagedIdentity.Tests;

public class TokenResponseTests
{
    [Fact]
    public void GetAccessToken()
    {
        var notBefore = DateTimeOffset.Now;
        var issuedAt = notBefore.AddMinutes(-1);
        var expiry = issuedAt.AddMinutes(5);

        var token = CreateAccessToken("Resource", notBefore, issuedAt, expiry);
        var accessToken = new AccessToken(token, expiry, null, tokenType: "Token");
        var response = new TokenResponse(accessToken);

        response.AccessToken.ShouldBe(token);
        response.Resource.ShouldBe("Resource");
        response.TokenType.ShouldBe("Token");
        response.ExpiresOn.ShouldBe(expiry.ToUnixTimeSeconds());
        response.NotBefore.ShouldBe(notBefore.ToUnixTimeSeconds());
        response.ExpiresIn.ShouldBe(expiry.ToUnixTimeSeconds() - issuedAt.ToUnixTimeSeconds());

        // Test serialization
        JsonSerializer.Serialize(response, AccessTokenJsonSerializerContext.Default.TokenResponse)
            .ShouldContain($"\"access_token\":\"{token}\"");
    }

    private static string CreateAccessToken(
        string audience,
        DateTimeOffset notBefore,
        DateTimeOffset issuedAt,
        DateTimeOffset expiry)
    {
        var handler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = audience,
            Subject = new ClaimsIdentity(),
            NotBefore = notBefore.UtcDateTime,
            IssuedAt = issuedAt.UtcDateTime,
            Expires = expiry.UtcDateTime,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(new byte[32]), SecurityAlgorithms.HmacSha256)
        };
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }
}