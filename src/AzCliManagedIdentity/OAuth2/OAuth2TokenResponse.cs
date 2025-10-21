using System.Text.Json.Serialization;
using AzCliManagedIdentity.ManagedIdentity;

namespace AzCliManagedIdentity.OAuth2;

public class OAuth2TokenResponse
{
    private readonly MsiTokenResponse _source;

    public OAuth2TokenResponse(MsiTokenResponse source)
    {
        _source = source;
    }

    public string TokenType => _source.TokenType;

    public string AccessToken => _source.AccessToken;

    [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
    public long ExpiresIn => _source.ExpiresIn;
}