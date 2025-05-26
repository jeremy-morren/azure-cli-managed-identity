using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using Azure.Core;

namespace AzCliManagedIdentity;

/// <summary>
/// The access token response from the Managed identity endpoint.
/// </summary>
/// <remarks>
/// See <see href="https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/how-to-use-vm-token#get-a-token-using-http"/>
/// </remarks>
public class TokenResponse
{
    private readonly AccessToken _accessToken;
    private readonly JwtSecurityToken _jwtToken;

    public TokenResponse(AccessToken accessToken)
    {
        _accessToken = accessToken;

        var handler = new JwtSecurityTokenHandler();
        _jwtToken = handler.ReadJwtToken(accessToken.Token);
    }

    public string? Resource => GetClaimValueString("aud");

    public string TokenType => _accessToken.TokenType;

    public long ExpiresOn => GetClaimValueLong("exp");

    public long NotBefore => GetClaimValueLong("nbf");

    public long ExpiresIn => ExpiresOn - GetClaimValueLong("iat");

    public string AccessToken => _accessToken.Token;
    
    private long GetClaimValueLong(string claimType)
    {
        var claim = _jwtToken.Claims.FirstOrDefault(c => c.Type == claimType);
        if (claim != null && long.TryParse(claim.Value, out var value))
            return value;
        return 0;
    }

    private string? GetClaimValueString(string claimType)
    {
        var claim = _jwtToken.Claims.FirstOrDefault(c => c.Type == claimType);
        return claim?.Value;
    }

    /// <summary>
    /// Not used by managed identities for Azure resources, included because it's part of the expected token response structure.
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
    public string RefreshToken => string.Empty;
}