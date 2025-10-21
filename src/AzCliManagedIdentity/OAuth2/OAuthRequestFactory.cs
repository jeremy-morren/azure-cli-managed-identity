using System.Diagnostics.CodeAnalysis;
using AzCliManagedIdentity.Api;
using AzCliManagedIdentity.ManagedIdentity;

namespace AzCliManagedIdentity.OAuth2;

/// <summary>
/// Factory for OAuth2 requests
/// </summary>
/// <remarks>
/// Allows acquiring an AzureAD token without providing credentials
/// </remarks>
public static class OAuthRequestFactory
{
    public const string OAuthTokenRequestPath = "/oauth2/token";

    /// <summary>
    /// Checks if the request is a valid OAuth2 token request
    /// </summary>
    /// <remarks>
    /// Gets the 'scope' parameter from the request body (and nothing else)
    /// </remarks>
    public static bool IsOAuth2TokenRequest(
        IFormCollection form,
        [MaybeNullWhen(false)] out TokenRequest token,
        out ErrorCode errorCode)
    {
        token = null;
        errorCode = ErrorCode.BadRequest;
        string? scope = form["scope"];
        if (string.IsNullOrEmpty(scope))
            return false;
        errorCode = (ErrorCode)(-1);

        token = new TokenRequest()
        {
            Resource = scope!
        };
        return true;
    }
}