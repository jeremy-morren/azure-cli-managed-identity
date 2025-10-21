using System.Text.Json;
using System.Text.Json.Serialization;
using AzCliManagedIdentity.ManagedIdentity;
using AzCliManagedIdentity.OAuth2;

namespace AzCliManagedIdentity.Api;

[JsonSourceGenerationOptions(JsonSerializerDefaults.General,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    IncludeFields = false)]
[JsonSerializable(typeof(MsiTokenResponse))]
[JsonSerializable(typeof(MsiErrorResponse))]
[JsonSerializable(typeof(OAuth2TokenResponse))]
[JsonSerializable(typeof(OAuth2ErrorResponse))]
public partial class JsonContext : JsonSerializerContext;