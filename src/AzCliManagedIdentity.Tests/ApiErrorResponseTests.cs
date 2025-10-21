using System.Net.Http.Json;
using AzCliManagedIdentity.Api;
using AzCliManagedIdentity.ManagedIdentity;
using Azure.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AzCliManagedIdentity.Tests;

[TestClass]
public class ApiErrorResponseTests
{
    [TestMethod]
    public async Task InvalidScopeShouldReturnInvalidScopeError()
    {
        const int port = 22580;
        await using var webApp = new ApiWebApp(port, MapEndpoints);

        await webApp.StartAsync();

        using var client = new HttpClient();
        client.BaseAddress = new Uri($"http://localhost:{port}/", UriKind.Absolute);

        const string resource = "invalid(scope)";
        var cloudShellRequest = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("token", UriKind.Relative),
            Content = new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                { "resource", resource }
            })
        };
        var virtualMachineRequest = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(
                $"metadata/identity/oauth2/token?api-version=2018-02-01&resource={Uri.EscapeDataString(resource)}",
                UriKind.Relative),
        };
        var oauth2Request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("oauth2/token", UriKind.Relative),
            Content = new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                { "scope", resource }
            })
        };

        foreach (var request in new[] { cloudShellRequest, virtualMachineRequest })
        {
            // Test missing metadata header fails
            using (var response = await client.SendAsync(request))
            {
                Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
                var error = await response.Content.ReadFromJsonAsync(TestJsonContext.Default.MsiErrorResponseDto);
                Assert.IsNotNull(error);
                Assert.AreEqual(ErrorResponseFactory.MetadataHeaderMissing, error.Error.Code);
            }

            var valid = new HttpRequestMessage()
            {
                Method = request.Method,
                RequestUri = request.RequestUri,
                Content = request.Content,
                Headers = { { "Metadata", "true" } }
            };
            using (var response = await client.SendAsync(valid))
            {
                Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
                var error = await response.Content.ReadFromJsonAsync(TestJsonContext.Default.MsiErrorResponseDto);
                Assert.IsNotNull(error);
                Assert.AreEqual(ErrorResponseFactory.InvalidScope, error.Error.Code);
            }
        }

        using (var response = await client.SendAsync(oauth2Request))
        {
            Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync(TestJsonContext.Default.OAuth2ErrorResponseDto);
            Assert.IsNotNull(error);
            Assert.AreEqual(ErrorResponseFactory.InvalidScope, error.Error);
        }
    }

    [TestMethod]
    public async Task AuthenticationFailedShouldReturn503()
    {
        const int port = 22581;
        await using var webApp = new ApiWebApp(port, MapEndpoints);

        await webApp.StartAsync();

        using var client = new HttpClient();
        client.BaseAddress = new Uri($"http://localhost:{port}/", UriKind.Absolute);

        const string resource = "https://management.azure.com//.default";
        var cloudShellRequest = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("token", UriKind.Relative),
            Content = new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                { "resource", resource }
            })
        };
        var virtualMachineRequest = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(
                $"metadata/identity/oauth2/token?api-version=2018-02-01&resource={Uri.EscapeDataString(resource)}",
                UriKind.Relative),
        };
        var oauth2Request = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("oauth2/token", UriKind.Relative),
            Content = new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                { "scope", resource }
            })
        };

        foreach (var request in new[] { cloudShellRequest, virtualMachineRequest })
        {
            request.Headers.Add("Metadata", "true");
            using var response = await client.SendAsync(request);
            Assert.AreEqual(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync(TestJsonContext.Default.MsiErrorResponseDto);
            Assert.IsNotNull(error);
            Assert.AreEqual(ErrorResponseFactory.CredentialUnavailable, error.Error.Code);
        }

        using (var response = await client.SendAsync(oauth2Request))
        {
            Assert.AreEqual(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync(TestJsonContext.Default.OAuth2ErrorResponseDto);
            Assert.IsNotNull(error);
            Assert.AreEqual(ErrorResponseFactory.CredentialUnavailable, error.Error);
        }
    }

    private static void MapEndpoints(IEndpointRouteBuilder app) =>
        new ApiPipeline(new FakeTokenService()).MapEndpoints(app);

    private class FakeTokenService : ITokenService
    {
        public Task<MsiTokenResponse> GetAccessToken(TokenRequest request, CancellationToken ct)
        {
            throw new AuthenticationFailedException("Simulated failure");
        }
    }
}