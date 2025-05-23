using System.Net;
using System.Text;
using System.Text.Json;
using AzCliManagedIdentity;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true; // Prevent the process from terminating.
    cts.Cancel();
};

/*
 * Exit codes:
 * 0 - Success
 * 1 - Error
 * 2 - Cancelled
 */
try
{
    var cgiRequest = CgiRequest.FromEnvironment();
    if (cgiRequest == null)
    {
        CgiResponse.WriteResponse(HttpStatusCode.BadRequest);
        return 1;
    }

    // Check if the request is a managed identity request
    if (!TokenRequest.TryCreateRequest(cgiRequest, out var tokenRequest, out var isBadRequest))
    {
        // Not found
        CgiResponse.WriteResponse(isBadRequest ? HttpStatusCode.BadRequest : HttpStatusCode.NotFound);
        return 0;
    }

    // For now, ObjectId, ClientId, and AzureResourceId are not supported
    if (tokenRequest.ObjectId != null || tokenRequest.ClientId != null || tokenRequest.AzureResourceId != null)
    {
        CgiResponse.WriteResponse(HttpStatusCode.NotImplemented);
        return 0;
    }

    // var service = new TokenService(
    //     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".azure"));

    // ~/.azure must be mounted in the container at /azureCli. TODO: Allow this to be configured.
    var service = new TokenService("/azureCli");

    var token = await service.RequestAzureCliToken(tokenRequest, cts.Token);
    var json = JsonSerializer.Serialize(token, AccessTokenJsonSerializerContext.Default.TokenResponse);
    CgiResponse.WriteResponse(HttpStatusCode.OK, contentType: "application/json", body: json);

    return 0;
}
catch (OperationCanceledException ex) when (ex.CancellationToken == cts.Token)
{
    return 2;
}
catch (Exception ex)
{
    CgiResponse.WriteError(ex.ToString());
    return 1;
}

static string SerializeEnvVars()
{
    var result = new StringBuilder();
    foreach (var keyObj in Environment.GetEnvironmentVariables().Keys)
    {
        if (keyObj is string key)
        {
            var value = Environment.GetEnvironmentVariable(key);
            result.AppendLine($"{key}={value}");
        }
    }

    return result.ToString();
}