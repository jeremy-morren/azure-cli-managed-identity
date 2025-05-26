using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var client = new HttpClient();
app.MapGet("/", async (context) =>
{
    var ct = context.RequestAborted;
    var credential = new DefaultAzureCredential();
    var token = await credential.GetTokenAsync(new TokenRequestContext(["https://management.azure.com/.default"]), ct);
    
    using var request = new HttpRequestMessage(HttpMethod.Get, "https://management.azure.com/subscriptions?api-version=2020-01-01");
    request.Headers.Authorization = new AuthenticationHeaderValue(token.TokenType, token.Token);
    var response = await client.SendAsync(request, ct);
    var content = await response.Content.ReadAsStringAsync(ct);
    await context.Response.WriteAsync(content);
});

app.Run();
