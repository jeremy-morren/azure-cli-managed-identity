using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using AzCliManagedIdentity.Api;
using AzCliManagedIdentity.ManagedIdentity;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AzCliManagedIdentity.Tests;

[TestClass]
public class ApiEndpointTests
{
    [TestMethod]
    public async Task GetCredentialAzureCloudShell()
    {
        const string resource = "a_resource";

        var token = CreateAccessToken(resource, 7985);

        const int port = 22380;
        await using var webApp = new ApiWebApp(port, app => MapEndpoints(app, resource, token));
        await webApp.StartAsync();

        Environment.SetEnvironmentVariable("MSI_ENDPOINT", $"http://localhost:{port}/token");
        var credential = new ManagedIdentityCredential();
        var result = await credential.GetTokenAsync(new TokenRequestContext([resource]));
        Assert.AreEqual(token.AccessToken, result.Token);
        Assert.AreEqual("Bearer", result.TokenType);
        Assert.AreEqual(token.ExpiresOn, result.ExpiresOn.ToUnixTimeSeconds());
    }

    [TestMethod]
    public async Task GetCredentialVirtualMachine()
    {
        const string resource = "http://localhost";

        var token = CreateAccessToken(resource, 551);

        const int port = 22381;
        await using var webApp = new ApiWebApp(port, app => MapEndpoints(app, resource, token));
        await webApp.StartAsync();

        using var client = new HttpClient();
        var request = new HttpRequestMessage()
        {
            RequestUri = new Uri(
                $"http://localhost:{port}/metadata/identity/oauth2/token?api-version=2018-02-01&resource={Uri.EscapeDataString(resource)}"),
            Method = HttpMethod.Get,
            Headers =
            {
                { "Metadata", "true" }
            }
        };
        using var httpResponse = await client.SendAsync(request);
        httpResponse.EnsureSuccessStatusCode();
        var tokenResponse = await httpResponse.Content.ReadFromJsonAsync(TestJsonContext.Default.MsiTokenResponseDto);
        Assert.IsNotNull(tokenResponse);
        Assert.AreEqual(token.AccessToken, tokenResponse.AccessToken);
        Assert.AreEqual("Bearer", tokenResponse.TokenType);
        Assert.AreEqual(resource, tokenResponse.Resource);
        Assert.AreEqual(token.ExpiresIn, tokenResponse.ExpiresIn);
        Assert.AreEqual(token.ExpiresOn, tokenResponse.ExpiresOn);
        Assert.AreEqual(token.NotBefore, tokenResponse.NotBefore);
        Assert.AreEqual(string.Empty, tokenResponse.RefreshToken);
    }

    [TestMethod]
    public async Task GetOAuth2Token()
    {
        const string resource = "oauth2scope";

        var token = CreateAccessToken(resource, 298);

        const int port = 22382;
        await using var webApp = new ApiWebApp(port, app => MapEndpoints(app, resource, token));
        await webApp.StartAsync();

        using var client = new HttpClient();
        var request = new HttpRequestMessage()
        {
            RequestUri = new Uri($"http://localhost:{port}/oauth2/token"),
            Method = HttpMethod.Post,
            Content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "scope", resource },
                { "grant_type", "client_credentials" }
            })
        };
        using var httpResponse = await client.SendAsync(request);
        httpResponse.EnsureSuccessStatusCode();
        var tokenResponse = await httpResponse.Content.ReadFromJsonAsync(TestJsonContext.Default.OAuth2TokenResponseDto);
        Assert.IsNotNull(tokenResponse);
        Assert.AreEqual(token.AccessToken, tokenResponse.AccessToken);
        Assert.AreEqual("Bearer", tokenResponse.TokenType);
        Assert.AreEqual(token.ExpiresIn.ToString(), tokenResponse.ExpiresIn);
    }

    private static MsiTokenResponse CreateAccessToken(string audience, int expiresIn)
    {
        var notBefore = DateTime.Parse("2055-10-08T05:07:09Z");
        var expiry = notBefore.AddSeconds(expiresIn);

        var handler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = audience,
            Subject = new ClaimsIdentity(),
            NotBefore = notBefore,
            IssuedAt = notBefore,
            Expires = expiry,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(new byte[32]), SecurityAlgorithms.HmacSha256)
        };
        var token = handler.CreateToken(tokenDescriptor);
        return new MsiTokenResponse(new AccessToken(handler.WriteToken(token), expiry));
    }

    private static void MapEndpoints(IEndpointRouteBuilder app, string resource, MsiTokenResponse token)
    {
        var tokenService = new FakeTokenService(resource, token);
        var apiPipeline = new ApiPipeline(tokenService);
        apiPipeline.MapEndpoints(app);
    }

    private class FakeTokenService : ITokenService
    {
        private readonly string _resource;
        private readonly MsiTokenResponse _token;

        public FakeTokenService(string resource, MsiTokenResponse token)
        {
            _resource = resource;
            _token = token;
        }

        public Task<MsiTokenResponse> GetAccessToken(TokenRequest request, CancellationToken ct)
        {
            Assert.AreEqual(_resource, request.Resource);
            return Task.FromResult(_token);
        }
    }
}