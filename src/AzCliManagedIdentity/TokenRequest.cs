using Azure.Core;

namespace AzCliManagedIdentity;

/// <summary>
/// A managed identity token request
/// </summary>
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

    public TokenRequestContext CreateTokenRequestContext() =>
        new(scopes: [Resource], parentRequestId: ClientRequestId);
}