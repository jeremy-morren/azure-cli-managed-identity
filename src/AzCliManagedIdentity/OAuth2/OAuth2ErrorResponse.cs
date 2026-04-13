namespace AzCliManagedIdentity.OAuth2;

/// <summary>
/// A response from an OAuth2 token endpoint indicating an error
/// </summary>
public record OAuth2ErrorResponse
{
    public required string Error { get; init; }

    public string? ErrorDescription { get; init; }

    public string? TraceId { get; init; }

    public string? CorrelationId { get; init; }

    public string? Timestamp { get; init; }
}