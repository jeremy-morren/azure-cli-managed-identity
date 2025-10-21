namespace AzCliManagedIdentity.ManagedIdentity;

/// <summary>
/// An MSI token endpoint error response
/// </summary>
public record MsiErrorResponse
{
    public required MsiErrorMessage Error { get; init; }
}

public record MsiErrorMessage(string Code, string? Message);

// {"error":{"code":"invalid_request","message":"Required audience parameter not specified"}}