using System.Text.Json.Serialization.Metadata;
using AzCliManagedIdentity.ManagedIdentity;
using AzCliManagedIdentity.OAuth2;
using Azure.Identity;
using Serilog;
using ILogger = Serilog.ILogger;

namespace AzCliManagedIdentity.Api;

public class ApiPipeline
{
    private readonly ITokenService _tokenService;
    private readonly ILogger _logger;

    public ApiPipeline(ITokenService tokenService)
    {
        _tokenService = tokenService;
        _logger = Log.ForContext<ApiPipeline>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // Map the cloud shell token request endpoint
        app.MapPost(TokenRequestFactory.CloudShellRequestPath, HandleCloudShellTokenRequest);

        // Map the virtual machine token request endpoint
        app.MapGet(TokenRequestFactory.VirtualMachineRequestPath, HandleVirtualMachineTokenRequest);

        // Map the OAuth2 token request endpoint
        app.MapPost(OAuthRequestFactory.OAuthTokenRequestPath, HandleOAuth2TokenRequest);
    }

    private async Task HandleCloudShellTokenRequest(HttpContext context)
    {
        var ct = context.RequestAborted;

        var response = context.Response;
        if (!context.Request.HasFormContentType)
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
        var form = await context.Request.ReadFormAsync(ct);
        if (TokenRequestFactory.TryCreateCloudShellRequest(context.Request, form, out var request, out var errorCode))
        {
            await HandleMsiTokenRequest(context, request);
        }
        else
        {
            // Bad request
            response.StatusCode = StatusCodes.Status400BadRequest;
            var error = ErrorResponseFactory.MsiError(errorCode);
            await response.WriteAsJsonAsync(error, JsonContext.Default.MsiErrorResponse, JsonContentType, ct);
        }
    }

    private async Task HandleVirtualMachineTokenRequest(HttpContext context)
    {
        var ct = context.RequestAborted;

        if (TokenRequestFactory.TryCreateVirtualMachineRequest(context.Request, out var request, out var errorCode))
        {
            await HandleMsiTokenRequest(context, request);
        }
        else
        {
            // Bad request
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var error = ErrorResponseFactory.MsiError(errorCode);
            await context.Response.WriteAsJsonAsync(error, JsonContext.Default.MsiErrorResponse, JsonContentType, ct);
        }
    }

    private async Task HandleOAuth2TokenRequest(HttpContext context)
    {
        var ct = context.RequestAborted;

        var response = context.Response;
        if (!context.Request.HasFormContentType)
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
        var form = await context.Request.ReadFormAsync(ct);

        if (OAuthRequestFactory.IsOAuth2TokenRequest(form, out var request, out var errorCode))
        {
            await HandleTokenRequest(context,
                request,
                r => new OAuth2TokenResponse(r),
                ErrorResponseFactory.OAuth2Error,
                ExceptionErrorResponseFactory.OAuth2Error,
                JsonContext.Default.OAuth2TokenResponse,
                JsonContext.Default.OAuth2ErrorResponse);
        }
        else
        {
            // Bad request
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var error = ErrorResponseFactory.OAuth2Error(errorCode);
            await context.Response.WriteAsJsonAsync(error, JsonContext.Default.OAuth2ErrorResponse, JsonContentType, ct);
        }
    }

    private async Task HandleTokenRequest<TResponse, TError>(
        HttpContext context,
        TokenRequest request,
        Func<MsiTokenResponse, TResponse> getResponse,
        Func<ErrorCode, TError> getErrorFromCode,
        Func<AuthenticationFailedException, ApiError<TError>> getErrorFromException,
        JsonTypeInfo<TResponse> responseTypeInfo,
        JsonTypeInfo<TError> errorTypeInfo)
    {
        var ct = context.RequestAborted;

        // For now, ObjectId, ClientId, and AzureResourceId are not supported
        if (request.ObjectId != null || request.ClientId != null || request.AzureResourceId != null)
        {
            context.Response.StatusCode = StatusCodes.Status501NotImplemented;
            return;
        }

        // Check that the scope is valid
        if (!ScopeUtilities.IsValidScope(request.Resource))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var error = getErrorFromCode(ErrorCode.InvalidScope);
            await context.Response.WriteAsJsonAsync(error, errorTypeInfo, JsonContentType, ct);
            return;
        }

        var remoteEndpoint = LoggingHelpers.GetRemoteEndpoint(context);
        if (remoteEndpoint != null)
            _logger.Information("Acquiring access token for {RemoteEndpoint}. Resource: {Resource}",
               remoteEndpoint,  request.Resource);
        else
            _logger.Information("Acquiring access token. Resource: {Resource}", request.Resource);

        try
        {
            var token = await _tokenService.GetAccessToken(request, ct);
            var response = getResponse(token);
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(response, responseTypeInfo, JsonContentType, ct);
        }
        catch (AuthenticationFailedException ex)
        {
            _logger.Error(ex, "Authentication failed while acquiring token for resource {Resource}", request.Resource);
            var (badRequest, response) = getErrorFromException(ex);
            context.Response.StatusCode = badRequest
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status503ServiceUnavailable; // For non-client errors, return 503 Service Unavailable
            await context.Response.WriteAsJsonAsync(response, errorTypeInfo, JsonContentType, ct);
        }
    }

    private Task HandleMsiTokenRequest(
        HttpContext context,
        TokenRequest request) =>
        HandleTokenRequest(context,
            request,
            r => r,
            ErrorResponseFactory.MsiError,
            ExceptionErrorResponseFactory.MsiError,
            JsonContext.Default.MsiTokenResponse,
            JsonContext.Default.MsiErrorResponse);

    private const string JsonContentType = "application/json";
}