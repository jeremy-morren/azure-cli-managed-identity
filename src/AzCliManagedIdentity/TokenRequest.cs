using System.Diagnostics.CodeAnalysis;
using System.Web;
using Azure.Core;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace AzCliManagedIdentity;

/// <summary>
/// A managed identity token request
/// </summary>
/// <remarks>
/// See <see href="https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/how-to-use-vm-token#get-a-token-using-http"/>
/// </remarks>
public record TokenRequest
{
    /// <summary>
    /// The <c>resource</c> query parameter.
    /// </summary>
    public required string Resource { get; init; }

    /// <summary>
    /// The parsed <c>api-version</c> parameter (provided as yyyy-MM-dd).
    /// </summary>
    public DateOnly? ApiVersion { get; init; }

    /// <summary>
    /// The <c>object_id</c> query parameter.
    /// </summary>
    public string? ObjectId { get; init; }

    /// <summary>
    /// The <c>client_id</c> query parameter.
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// The <c>msi_res_id</c> query parameter.
    /// </summary>
    public string? AzureResourceId { get; init; }

    /// <summary>
    /// The <c>x-ms-client-request-id</c> header (sent by <see cref="Azure.Identity.ManagedIdentityCredential"/>).
    /// </summary>
    public string? ClientRequestId { get; init; }

    /// <summary>
    /// The <c>x-ms-return-client-request-id</c> header (sent by <see cref="Azure.Identity.ManagedIdentityCredential"/>).
    /// </summary>
    public bool? ReturnClientRequestId { get; init; }

    public TokenRequestContext CreateTokenRequestContext()
    {
        return new TokenRequestContext(scopes: [Resource], parentRequestId: ClientRequestId);
    }

    #region Create

    /// <summary>
    /// Try to create a <see cref="TokenRequest"/> from the CGI request.
    /// </summary>
    public static bool TryCreateRequest(
        CgiRequest cgiRequest,
        [MaybeNullWhen(false)] out TokenRequest tokenRequest,
        out ErrorCode errorCode)
    {
        tokenRequest = null;
        errorCode = ErrorCode.None;

        if (TryCreateDefaultRequest(cgiRequest, out tokenRequest, out errorCode))
            return true;

        if (TryCreateCloudShellRequest(cgiRequest, out tokenRequest, out errorCode))
            return true;

        return false;
    }

    /// <summary>
    /// Checks if the request URL is a managed identity request.
    /// </summary>
    public static bool TryCreateDefaultRequest(
        CgiRequest cgiRequest,
        [MaybeNullWhen(false)] out TokenRequest tokenRequest,
        out ErrorCode errorCode)
    {
        tokenRequest = null;
        errorCode = ErrorCode.None;

        // Method must be GET
        if (cgiRequest.Method != HttpMethod.Get)
            return false;

        if (!cgiRequest.IsPath("/metadata/identity/oauth2/token"))
            return false;

        // Check for header 'Metadata: true'
        if (!HasMetadataHeader(cgiRequest))
        {
            errorCode = ErrorCode.MetadataHeaderMissing;
            return false;
        }

        var query = ParseQuery(cgiRequest.RequestUri.Query);

        // Ensure api version is present and valid
        if (!query.TryGetValue("api-version", out var apiVersion)
            || !DateOnly.TryParseExact(apiVersion, "yyyy-MM-dd", out var apiVersionValue)
            || apiVersionValue < new DateOnly(2018, 2, 1))
        {
            errorCode = ErrorCode.BadRequest;
            return false;
        }

        if (!query.TryGetValue("resource", out var resource) || string.IsNullOrEmpty(resource))
        {
            errorCode = ErrorCode.ResourceNotSpecified;
            return false;
        }

        tokenRequest = new TokenRequest()
        {
            ApiVersion = apiVersionValue,
            Resource = resource,
            ObjectId = query.GetValueOrDefault("object_id"),
            ClientId = query.GetValueOrDefault("client_id"),
            AzureResourceId = query.GetValueOrDefault("msi_res_id"),
            
            ClientRequestId = cgiRequest.Headers.GetValueOrDefault("x-ms-client-request-id"),
            ReturnClientRequestId = cgiRequest.Headers.TryGetValue("x-ms-return-client-request-id", out var returnClientRequestId)
                ? bool.TryParse(returnClientRequestId, out var returnClientRequestIdValue)
                    ? returnClientRequestIdValue
                    : null
                : null
        };
        return true;
    }

    /// <summary>
    /// When the request is a cloud shell request, ManagedIdentityCredential switches to a FORM post request.
    /// </summary>
    /// <remarks>
    /// <see cref="Microsoft.Identity.Client.ManagedIdentity.CloudShellManagedIdentitySource.CreateRequest"/>
    /// </remarks>
    public static bool TryCreateCloudShellRequest(CgiRequest cgiRequest,
        [MaybeNullWhen(false)] out TokenRequest request,
        out ErrorCode errorCode)
    {
        request = null;
        errorCode = ErrorCode.None;

        // Method must be POST
        if (cgiRequest.Method != HttpMethod.Post)
            return false;

        // We are going to assume that the request URL should be '/oauth2/token' (that's what it is on Azure Cloud Shell)
        if (!cgiRequest.IsPath("/oauth2/token"))
            return false;

        // Check for FORM urlencoded content type
        if (!Match(cgiRequest.ContentType, "application/x-www-form-urlencoded"))
            return false;

        // Check for header 'Metadata: true'
        if (!HasMetadataHeader(cgiRequest))
        {
            errorCode = ErrorCode.MetadataHeaderMissing;
            return false;
        }

        var body = cgiRequest.ReadFormUrlEncodedBody();
        if (!body.TryGetValue("resource", out var resource) || string.IsNullOrEmpty(resource))
        {
            errorCode = ErrorCode.ResourceNotSpecified;
            return false;
        }

        GetMsRequestHeaders(cgiRequest, out var clientRequestId, out var returnClientRequestId);

        request = new TokenRequest()
        {
            Resource = resource!,
            ClientRequestId = clientRequestId,
            ReturnClientRequestId = returnClientRequestId,
        };
        return true;
    }

    private static bool Match(string? left, string? right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static Dictionary<string, string?> ParseQuery(string query)
    {
        var collection = HttpUtility.ParseQueryString(query);
        var result = new Dictionary<string, string?>(capacity: collection.Count, StringComparer.OrdinalIgnoreCase);
        foreach (string key in collection.Keys)
            result[key] = collection[key];
        return result;
    }
    
    /// <summary>
    /// Checks whether the request has the <c>Metadata</c> header set to <c>true</c>.
    /// </summary>
    public static bool HasMetadataHeader(CgiRequest cgiRequest) => 
        cgiRequest.Headers.TryGetValue("Metadata", out var metadata) && metadata == "true";

    /// <summary>
    /// Gets values of <c>x-ms-client-request-id</c> and <c>x-ms-return-client-request-id</c> headers.
    /// </summary>
    private static void GetMsRequestHeaders(CgiRequest cgiRequest, out string? clientRequestId, out bool? returnClientRequestId)
    {
        clientRequestId = cgiRequest.Headers.GetValueOrDefault("x-ms-client-request-id");
        returnClientRequestId = cgiRequest.Headers.TryGetValue("x-ms-return-client-request-id", out var returnHeader)
            ? bool.TryParse(returnHeader, out var returnValue)
                ? returnValue
                : null
            : null;
    }

    #endregion
}