namespace AzCliManagedIdentity;

/// <summary>
/// A token endpoint error response
/// </summary>
public record ErrorResponse
{
    public required ErrorMessage Error { get; init; }
}

public record ErrorMessage(string Code, string Message);

// {"error":{"code":"invalid_request","message":"Required audience parameter not specified"}}