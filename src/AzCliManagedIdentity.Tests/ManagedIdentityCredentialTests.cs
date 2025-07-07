using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Serilog;
using Serilog.Events;
using Xunit.Abstractions;

namespace AzCliManagedIdentity.Tests;

public class ManagedIdentityCredentialTests(ITestOutputHelper output)
{
    [Fact]
    public async Task GetCredentialAzureCloudShell()
    {
        const string resource = "a resource";

        var token = CreateAccessToken(resource);
        var tokenService = new Mock<ITokenService>(MockBehavior.Strict);
        tokenService.Setup(s => s.RequestAzureCliToken(
                It.Is<TokenRequest>(r => r.Resource == resource),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(token)
            .Verifiable();

        const int port = 22380;
        await using var webApp = new WebApp(port, tokenService.Object, output);
        await webApp.StartAsync();

        Environment.SetEnvironmentVariable("MSI_ENDPOINT", $"http://localhost:{port}/oauth2/token");
        var credential = new ManagedIdentityCredential();
        var result = await credential.GetTokenAsync(new TokenRequestContext([resource]));
        result.Token.ShouldBe(token.AccessToken);
    }

    [Fact]
    public async Task GetCredentialVirtualMachine()
    {
        const string resource = "http://localhost";

        var token = CreateAccessToken(resource);
        var tokenService = new Mock<ITokenService>(MockBehavior.Strict);
        tokenService.Setup(s => s.RequestAzureCliToken(
                It.Is<TokenRequest>(r => r.Resource == resource),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(token)
            .Verifiable();

        const int port = 22381;
        await using var webApp = new WebApp(port, tokenService.Object, output);
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
        var jsonOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
        var tokenResponse = await httpResponse.Content.ReadFromJsonAsync<TokenResponseDto>(jsonOptions);
        tokenResponse.ShouldNotBeNull();
        tokenResponse.AccessToken.ShouldBe(token.AccessToken);
        tokenResponse.TokenType.ShouldBe("Bearer");
    }

    private class WebApp : IAsyncDisposable
    {
        private readonly WebApplication _app;

        public WebApp(
            int port,
            ITokenService tokenService,
            ITestOutputHelper output,
            LogEventLevel logLevel = LogEventLevel.Warning)
        {
            var builder = WebApplication.CreateSlimBuilder(["--environment=Development"]);
            builder.WebHost.UseKestrel(o => o.ListenAnyIP(port));

            var logger = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel)
                .WriteTo.TestOutput(output, logLevel)
                .CreateLogger();
            builder.Host.UseSerilog(logger);

            _app = builder.Build();
            _app.UseSerilogRequestLogging();
            new ApiPipeline(tokenService).MapEndpoints(_app);
        }

        public Task StartAsync() => _app.StartAsync();

        public ValueTask DisposeAsync() => _app.DisposeAsync();
    }

    private static TokenResponse CreateAccessToken(string audience, int expiresIn = 3600)
    {
        var notBefore = DateTime.UtcNow;
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
        return new TokenResponse(new AccessToken(handler.WriteToken(token), expiry));
    }

    private record TokenResponseDto(string AccessToken, string TokenType);
}