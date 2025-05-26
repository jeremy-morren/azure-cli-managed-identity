using System.Net;
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

    // Handle healthcheck requests
    if (Healthcheck.IsHealthcheck(cgiRequest))
    {
        await Healthcheck.Invoke(cts.Token);
        return 0;
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

    // Try to get the token, write error if it fails
    try
    {
        var token = await TokenService.RequestAzureCliToken(tokenRequest, cts.Token);
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