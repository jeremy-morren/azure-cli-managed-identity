using Azure.Identity;

namespace AzCliManagedIdentity;

public class ApiPipeline
{
    private readonly ITokenService _tokenService;

    public ApiPipeline(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // Map the healthcheck endpoint
        app.MapGet("/healthz", Healthcheck.Handle);

        // Map the token request endpoint
        app.MapPost(TokenRequestFactory.CloudShellRequestPath, HandleCloudShellTokenRequest);

        // Map the virtual machine token request endpoint
        app.MapGet(TokenRequestFactory.VirtualMachineRequestPath, HandleVirtualMachineTokenRequest);
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
            await HandleTokenRequest(context, request);
        }
        else
        {
            // Bad request
            response.StatusCode = StatusCodes.Status400BadRequest;
            var error = ErrorResponseFactory.FromCode(errorCode);
            await response.WriteAsJsonAsync(error, ApiJsonSerializerContext.Default.ErrorResponse, JsonContentType, ct);
        }
    }

    private async Task HandleVirtualMachineTokenRequest(HttpContext context)
    {
        var ct = context.RequestAborted;

        if (TokenRequestFactory.TryCreateVirtualMachineRequest(context.Request, out var request, out var errorCode))
        {
            await HandleTokenRequest(context, request);
        }
        else
        {
            // Bad request
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var error = ErrorResponseFactory.FromCode(errorCode);
            await context.Response.WriteAsJsonAsync(error,
                ApiJsonSerializerContext.Default.ErrorResponse,
                JsonContentType,
                ct);
        }

        // Not found
    }

    private async Task HandleTokenRequest(HttpContext context, TokenRequest request)
    {
        var ct = context.RequestAborted;

        // For now, ObjectId, ClientId, and AzureResourceId are not supported
        if (request.ObjectId != null || request.ClientId != null || request.AzureResourceId != null)
        {
            context.Response.StatusCode = StatusCodes.Status501NotImplemented;
            return;
        }

        try
        {
            var result = await _tokenService.RequestAzureCliToken(request, ct);
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = JsonContentType;
            await context.Response.WriteAsJsonAsync(result,
                ApiJsonSerializerContext.Default.TokenResponse,
                JsonContentType,
                ct);
        }
        catch (AuthenticationFailedException ex)
        {
            var error = ExceptionErrorResponseFactory.FromException(ex);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(error,
                ApiJsonSerializerContext.Default.ErrorResponse,
                JsonContentType,
                ct);
        }
    }

    private const string JsonContentType = "application/json";
}