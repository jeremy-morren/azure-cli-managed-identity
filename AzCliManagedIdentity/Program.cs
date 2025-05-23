// using System.Diagnostics;
// using Azure.Core;
// using Azure.Identity;
//
// var psi = new ProcessStartInfo()
// {
//     FileName = "/bin/sh",
//     Arguments = "-c \"az --version\"",
//     UseShellExecute = false,
//     ErrorDialog = false,
//     CreateNoWindow = true,
//     RedirectStandardOutput = true
// };
//
// try
// {
//     // using var process = Process.Start(psi)!;
//     // process.WaitForExit();
//     //
//     // var output = process.StandardOutput.ReadToEnd();
//     // Console.WriteLine(output);
//     // return process.ExitCode;
//     
//     var context = new TokenRequestContext(["https://management.azure.com/.default"]);
//     var token = new AzureCliCredential().GetToken(context);
//     return 0;
//
// }
// catch (Exception ex)
// {
//     Console.WriteLine(ex);
//     return 1;
// }
//

using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using AzCliManagedIdentity;
using Azure.Identity;

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
    if (!TokenRequest.TryCreateRequest(cgiRequest, out var tokenRequest, out var errorCode))
    {
        if (errorCode == ErrorCode.None)
        {
            // Not found
            CgiResponse.WriteResponse(HttpStatusCode.NotFound);
            return 0;
        }
        
        // Bad request
        var error = ErrorResponseFactory.FromCode(errorCode);
        CgiResponse.WriteJsonResponse(HttpStatusCode.BadRequest,
            error,
            AccessTokenJsonSerializerContext.Default.ErrorResponse);
        
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

    // Try to get the token, write error if it fails
    try
    {
        var token = await service.RequestAzureCliToken(tokenRequest, cts.Token);
        CgiResponse.WriteJsonResponse(HttpStatusCode.OK,
            token,
            AccessTokenJsonSerializerContext.Default.TokenResponse);
    }
    catch (AuthenticationFailedException ex)
    {
        var error = ExceptionErrorResponseFactory.FromException(ex);
        // Write error to standard error for logging
        Console.Error.WriteLine($"{ex.GetType().FullName}: {ex.Message}");
        CgiResponse.WriteJsonResponse(HttpStatusCode.BadRequest,
            error,
            AccessTokenJsonSerializerContext.Default.ErrorResponse);
    }

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