using AzCliManagedIdentity.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AzCliManagedIdentity.Tests;

[TestClass]
public class HealthcheckTests
{
    [TestMethod]
    public async Task RunCliShouldSucceed()
    {
        var healthcheck = new CliHealthcheck("dotnet --list-sdks");
        var (statusCode, output) = await healthcheck.CheckHealth(CancellationToken.None);
        Assert.AreEqual(StatusCodes.Status200OK, statusCode, output);
        Assert.AreEqual("dotnet --list-sdks exited with code 0", output);
    }

    [TestMethod]
    public async Task MapHealthcheckEndpoint()
    {
        const int port = 22480;
        await using var app = new ApiWebApp(port, app =>
        {
            var success = new CliHealthcheck("dotnet --list-sdks");
            app.MapGet("/success", success.Handle);

            var failure = new CliHealthcheck("nonexistent-command --foo");
            app.MapGet("/failure", failure.Handle);
        });
        await app.StartAsync();

        using var client = new HttpClient();
        using (var response = await client.GetAsync($"http://localhost:{port}/success"))
        {
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            Assert.AreEqual("dotnet --list-sdks exited with code 0", body);
        }
        using (var response = await client.GetAsync($"http://localhost:{port}/failure"))
        {
            Assert.AreEqual(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            StringAssert.Contains(body, "nonexistent-command --foo");
        }
    }
}