using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzCliManagedIdentity.Tests;

[JsonSourceGenerationOptions(JsonSerializerDefaults.General,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    IncludeFields = false)]
[JsonSerializable(typeof(MsiTokenResponseDto))]
[JsonSerializable(typeof(MsiErrorResponseDto))]
[JsonSerializable(typeof(OAuth2TokenResponseDto))]
[JsonSerializable(typeof(OAuth2ErrorResponseDto))]
public partial class TestJsonContext : JsonSerializerContext
{
    
}

public record MsiTokenResponseDto
{
    public required string AccessToken { get; init; }
    public required string TokenType { get; init; }
    public required long ExpiresOn { get; init; }
    public required long NotBefore { get; init; }
    public required long ExpiresIn { get; init; }
    public string? Resource { get; init; }
    public required string RefreshToken { get; init; }
}

public record OAuth2TokenResponseDto
{
    public required string AccessToken { get; init; }
    public required string TokenType { get; init; }
    public required string ExpiresIn { get; init; }
}


public record MsiErrorResponseDto
{
    public required MsiErrorMessageDto Error { get; init; }
}

public record MsiErrorMessageDto
{
    public required string Code { get; init; }
}

public record OAuth2ErrorResponseDto
{
    public required string Error { get; init; }
}