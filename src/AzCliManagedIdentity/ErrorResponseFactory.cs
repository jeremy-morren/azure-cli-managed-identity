// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
namespace AzCliManagedIdentity;

public static class ErrorResponseFactory
{
    public static ErrorResponse FromCode(ErrorCode code) => new()
    {
        Error = CreateErrorMessage(code)
    };

    private static ErrorMessage CreateErrorMessage(ErrorCode code) => code switch
    {
        ErrorCode.MetadataHeaderMissing =>
            new("bad_request_102", "Required metadata header not specified"),
        ErrorCode.ResourceNotSpecified =>
            new ErrorMessage("invalid_request", "Required audience parameter not specified"),
        ErrorCode.BadRequest =>
            new ErrorMessage("invalid_request", "Invalid request"),
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
    };
}