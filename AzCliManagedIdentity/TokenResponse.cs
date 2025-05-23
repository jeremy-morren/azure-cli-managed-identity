using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
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

    [JsonPropertyName("resource")]
    public string? Resource => GetClaimValueString("aud");

    [JsonPropertyName("access_token")]
    public string AccessToken => _accessToken.Token;

    [JsonPropertyName("token_type")]
    public string TokenType => _accessToken.TokenType;

    [JsonPropertyName("expires_on")]
    public long ExpiresOn => GetClaimValueLong("exp");

    [JsonPropertyName("not_before")]
    public long NotBefore => GetClaimValueLong("nbf");

    [JsonPropertyName("expires_in")]
    public long ExpiresIn => ExpiresOn - GetClaimValueLong("iat");

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
}