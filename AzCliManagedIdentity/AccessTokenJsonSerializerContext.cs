using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzCliManagedIdentity;

[JsonSourceGenerationOptions(JsonSerializerDefaults.General,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    IncludeFields = false)]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(ErrorResponse))]
public partial class AccessTokenJsonSerializerContext : JsonSerializerContext
{
    
}