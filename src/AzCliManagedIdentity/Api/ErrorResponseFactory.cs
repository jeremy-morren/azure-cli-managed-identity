// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using AzCliManagedIdentity.ManagedIdentity;
using AzCliManagedIdentity.OAuth2;

namespace AzCliManagedIdentity.Api;

public static class ErrorResponseFactory
{
    public static MsiErrorResponse MsiError(ErrorCode code) => new()
    {
        Error = CreateErrorMessage(code)
    };

    public static OAuth2ErrorResponse OAuth2Error(ErrorCode code)
    {
        var error = CreateErrorMessage(code);
        return new OAuth2ErrorResponse()
        {
            Error = error.Code,
            ErrorDescription = error.Message
        };
    }

    private static MsiErrorMessage CreateErrorMessage(ErrorCode code) => code switch
    {
        ErrorCode.InvalidScope =>
            new(InvalidScope, "The specified scope is not in expected format. Only alphanumeric characters, '.', '-', ':', '_', and '/' are allowed"),
        ErrorCode.MetadataHeaderMissing =>
            new(MetadataHeaderMissing, "Required metadata header not specified"),
        ErrorCode.ResourceNotSpecified =>
            new MsiErrorMessage(BadRequest, "Required audience parameter not specified"),
        ErrorCode.BadRequest =>
            new MsiErrorMessage(BadRequest, "Invalid request"),
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
    };

    /// <summary>
    /// General error code indicating a bad request
    /// </summary>
    public const string BadRequest = "invalid_request";

    /// <summary>
    /// Error code indicating that the metadata header is missing
    /// </summary>
    public const string MetadataHeaderMissing = "bad_request_102";

    /// <summary>
    /// Error code indicating that the specified scope is invalid
    /// </summary>
    public const string InvalidScope = "invalid_scope";

    /// <summary>
    /// Error code indicating that the credential is unavailable
    /// </summary>
    public const string CredentialUnavailable = "credential_unavailable";
}