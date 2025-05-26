using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;

namespace AzCliManagedIdentity.Tests;

/// <summary>
/// MSI endpoint is at <see cref="Microsoft.Identity.Client.ManagedIdentity.EnvironmentVariables.MsiEndpoint"/>.
/// See <see cref=" Microsoft.Identity.Client.ManagedIdentity.ManagedIdentityClient.GetManagedIdentitySource"/>
/// </summary>
public class ManagedIdentityTests
{
    [Fact]
    public void GetCredentialLocal()
    {
        Environment.SetEnvironmentVariable("MSI_ENDPOINT", "http://localhost:8080/oauth2/token");
        // Environment.SetEnvironmentVariable("MSI_ENDPOINT", "http://localhost:8080");
        var options = new ManagedIdentityCredentialOptions()
        {
            Retry =
            {
                MaxRetries = 0
            },
            // Transport = new TestTransport()
        };
        var credential = new ManagedIdentityCredential(options);
        var token = credential.GetToken(new TokenRequestContext(["https://management.azure.com/.default"]));
        token.ExpiresOn.ShouldBeGreaterThan(DateTimeOffset.Now);
    }

    private class TestTransport : HttpPipelineTransport
    {
        public override void Process(HttpMessage message)
        {
            throw new NotImplementedException();
        }

        public override ValueTask ProcessAsync(HttpMessage message)
        {
            throw new NotImplementedException();
        }

        public override Request CreateRequest() => Shared.CreateRequest();

        private static readonly HttpClientTransport Shared = new HttpClientTransport();
    }
}