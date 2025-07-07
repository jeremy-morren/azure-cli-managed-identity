using System.Diagnostics.CodeAnalysis;

namespace AzCliManagedIdentity;

public static class TokenRequestFactory
{
    private const ErrorCode NoError = (ErrorCode)(-1);

    /// <summary>
    /// Request path for token requests as used by Azure Cloud Shell.
    /// </summary>
    public const string CloudShellRequestPath = "/oauth2/token";

    /// <summary>
    /// Creates a token request from a token request as used by Azure Cloud Shell.
    /// </summary>
    /// <remarks>
    /// <see cref="Microsoft.Identity.Client.ManagedIdentity.CloudShellManagedIdentitySource.CreateRequest"/>
    /// </remarks>
    public static bool TryCreateCloudShellRequest(HttpRequest request,
        IFormCollection form,
        [MaybeNullWhen(false)] out TokenRequest tokenRequest,
        out ErrorCode errorCode)
    {
        tokenRequest = null;

        // Check for header 'Metadata: true'
        if (!HasMetadataHeader(request))
        {
            errorCode = ErrorCode.MetadataHeaderMissing;
            return false;
        }

        if (!form.TryGetValue("resource", out var resource) || string.IsNullOrEmpty(resource))
        {
            errorCode = ErrorCode.ResourceNotSpecified;
            return false;
        }

        tokenRequest = new TokenRequest()
        {
            Resource = resource!,
            ClientRequestId = request.Headers.GetValueOrDefault("x-ms-client-request-id"),
            ReturnClientRequestId = bool.TryParse(request.Headers.GetValueOrDefault("x-ms-return-client-request-id"), out var b)
                ? b
                : null
        };
        errorCode = NoError;
        return true;
    }

    /// <summary>
    /// Request path for managed identity requests as used by Azure Virtual Machines.
    /// </summary>
    public const string VirtualMachineRequestPath = "/metadata/identity/oauth2/token";

    /// <summary>
    /// Checks if the request URL is a managed identity request as used by Azure Virtual Machines
    /// </summary>
    /// <remarks>
    /// See <see href="https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/how-to-use-vm-token#get-a-token-using-http"/>
    /// </remarks>
    public static bool TryCreateVirtualMachineRequest(
        HttpRequest request,
        [MaybeNullWhen(false)] out TokenRequest tokenRequest,
        out ErrorCode errorCode)
    {
        tokenRequest = null;

        if (!request.HasMetadataHeader())
        {
            errorCode = ErrorCode.MetadataHeaderMissing;
            return false;
        }

        // Ensure api-version parameter is present and valid
        if (!DateOnly.TryParseExact(request.Query.GetValueOrDefault("api-version"), "yyyy-MM-dd", out var apiVersion)
            || apiVersion < new DateOnly(2018, 2, 1))
        {
            errorCode = ErrorCode.BadRequest;
            return false;
        }

        if (!request.Query.TryGetValue("resource", out var resource) || string.IsNullOrEmpty(resource))
        {
            errorCode = ErrorCode.ResourceNotSpecified;
            return false;
        }

        tokenRequest = new TokenRequest()
        {
            ApiVersion = apiVersion,
            Resource = resource!,
            ObjectId = request.Query.GetValueOrDefault("object_id"),
            ClientId = request.Query.GetValueOrDefault("client_id"),
            AzureResourceId = request.Query.GetValueOrDefault("msi_res_id"),

            ClientRequestId = request.Headers.GetValueOrDefault("x-ms-client-request-id"),
            ReturnClientRequestId =
                bool.TryParse(request.Headers.GetValueOrDefault("x-ms-return-client-request-id"),
                    out var returnClientRequestId)
                    ? returnClientRequestId
                    : null
        };
        errorCode = NoError;
        return true;
    }

    /// <summary>
    /// Checks whether the request has the <c>Metadata</c> header set to <c>true</c>.
    /// </summary>
    private static bool HasMetadataHeader(this HttpRequest request) =>
        request.Headers.GetValueOrDefault("Metadata") == "true";

    /// <summary>
    /// Gets the value of a header parameter or returns null if it does not exist.
    /// </summary>
    private static string? GetValueOrDefault(this IHeaderDictionary headers, string key) =>
        headers.TryGetValue(key, out var value) ? value.ToString() : null;

    /// <summary>
    /// Gets the value of a query parameter or returns null if it does not exist.
    /// </summary>
    private static string? GetValueOrDefault(this IQueryCollection query, string key) =>
        query.TryGetValue(key, out var value) ? value.ToString() : null;
}