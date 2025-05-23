using System.Diagnostics.CodeAnalysis;
using System.Net;
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
    /// The parsed <c>api-version</c> parameter (provided as yyyy-MM-dd).
    /// </summary>
    public required DateOnly ApiVersion { get; init; }

    /// <summary>
    /// The <c>resource</c> query parameter.
    /// </summary>
    public required string Resource { get; init; }

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
    /// Checks if the request URL is a managed identity request.
    /// </summary>
    public static bool TryCreateDefaultRequest(
        CgiRequest cgiRequest,
        [MaybeNullWhen(false)] out TokenRequest tokenRequest,
        [NotNullWhen(false)] out HttpStatusCode? errorCode)
    {
        tokenRequest = null;
        errorCode = null;

        // Method must be GET
        if (!Match(cgiRequest.Method, "GET"))
        {
            errorCode = HttpStatusCode.NotFound;
            return false;
        }

        if (!Match(cgiRequest.RequestUri.AbsolutePath, "/metadata/identity/oauth2/token"))
        {
            errorCode = HttpStatusCode.NotFound;
            return false;
        }

        // Check for header 'Metadata: true'
        if (!cgiRequest.Headers.TryGetValue("Metadata", out var metadata) || metadata != "true")
        {
            errorCode = HttpStatusCode.BadRequest;
            return false;
        }

        var query = ParseQuery(cgiRequest.RequestUri.Query);
        if (!query.TryGetValue("api-version", out var apiVersion)
            || !DateOnly.TryParseExact(apiVersion, "yyyy-MM-dd", out var apiVersionValue)
            || apiVersionValue < new DateOnly(2018, 2, 1))
        {
            errorCode = HttpStatusCode.BadRequest;
            return false;
        }

        if (!query.TryGetValue("resource", out var resource) || string.IsNullOrEmpty(resource))
        {
            errorCode = HttpStatusCode.BadRequest;
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
    public static bool TryCreateCloudShellRequest(CgiRequest cgiRequest, [MaybeNullWhen(false)] out TokenRequest request)
    {
        request = null;

        // Method must be POST
        if (!Match(cgiRequest.Method, "POST"))
            return false;

        // We are going to assume that the request URL should be '/oauth2/token' (that's what it is on Azure Cloud Shell)
        if (!Match(cgiRequest.RequestUri.AbsolutePath, "/oauth2/token"))
            return false;

        // Check for header 'Metadata: true'
        if (!cgiRequest.Headers.TryGetValue("Metadata", out var metadata) || metadata != "true")
            return false;

        // Check for header 'ContentType: application/x-www-form-urlencoded'
        if (!cgiRequest.Headers.TryGetValue("ContentType", out var contentType) || contentType != "application/x-www-form-urlencoded")
            return false;

        throw new NotImplementedException();
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

    #endregion
}