using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using AzCliManagedIdentity.ManagedIdentity;
using Azure.Core;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AzCliManagedIdentity.Tests;

[TestClass]
public class TokenResponseTests
{
    [TestMethod]
    public void GetAccessToken()
    {
        var notBefore = DateTimeOffset.Now;
        var issuedAt = notBefore.AddMinutes(-1);
        var expiry = issuedAt.AddMinutes(5);

        var token = CreateAccessToken("Resource", notBefore, issuedAt, expiry);
        var accessToken = new AccessToken(token, expiry, null, tokenType: "Token");
        var response = new MsiTokenResponse(accessToken);

        Assert.AreEqual(response.AccessToken, token);
        Assert.AreEqual(response.Resource, "Resource");
        Assert.AreEqual(response.TokenType, "Token");
        Assert.AreEqual(response.ExpiresOn, expiry.ToUnixTimeSeconds());
        Assert.AreEqual(response.NotBefore, notBefore.ToUnixTimeSeconds());
        Assert.AreEqual(response.ExpiresIn, expiry.ToUnixTimeSeconds() - issuedAt.ToUnixTimeSeconds());

        // Test serialization
        var json = JsonSerializer.Serialize(response, Api.JsonContext.Default.MsiTokenResponse);
        Assert.IsTrue(json.Contains($"\"access_token\":\"{token}\""));
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