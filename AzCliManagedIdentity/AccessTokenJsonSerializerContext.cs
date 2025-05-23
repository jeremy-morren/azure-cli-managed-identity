using System.Text.Json.Serialization;

namespace AzCliManagedIdentity;

[JsonSerializable(typeof(TokenResponse))]
public partial class AccessTokenJsonSerializerContext : JsonSerializerContext
{
    
}