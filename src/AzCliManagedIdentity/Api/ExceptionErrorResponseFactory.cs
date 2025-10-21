using System.Text.RegularExpressions;
using AzCliManagedIdentity.ManagedIdentity;
using AzCliManagedIdentity.OAuth2;
using Azure.Identity;
// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident

namespace AzCliManagedIdentity.Api;

public static partial class ExceptionErrorResponseFactory
{
    public static ApiError<MsiErrorResponse> MsiError(AuthenticationFailedException ex)
    {
        var error = OAuth2Error(ex);
        return new(
            error.BadRequest,
            new MsiErrorResponse()
            {
                Error = new MsiErrorMessage(
                    error.Response.Error,
                    error.Response.ErrorDescription)
            });
    }

    public static ApiError<OAuth2ErrorResponse> OAuth2Error(AuthenticationFailedException ex)
    {
        if (ex.InnerException is AggregateException aex)
        {
            foreach (var ie in aex.InnerExceptions)
            {
                if (OAuth2ErrorMessage(ie) is { } ie1)
                    return ie1;
                if (InteractiveAuthenticationRequired(ie) is { } ie2)
                    return ie2;
            }
        }

        var e = ex.InnerException ?? ex;

        if (OAuth2ErrorMessage(e) is { } e1)
            return e1;
        if (InteractiveAuthenticationRequired(e) is { } e2)
            return e2;

        // Unknown error, most likely not logged in, hence not a bad request
        return new(
            false,
            new OAuth2ErrorResponse()
            {
                Error = ErrorResponseFactory.CredentialUnavailable,
                ErrorDescription = e.Message
            });
    }

    private static ApiError<OAuth2ErrorResponse>? OAuth2ErrorMessage(Exception ex)
    {
        var oauth2Error = OAuth2ErrorRegex().Match(ex.Message);
        if (!oauth2Error.Success) return null;

        var code = oauth2Error.Groups["Code"].Value;
        var response = new OAuth2ErrorResponse()
        {
            Error = code switch
            {
                "AADSTS500011" => ErrorResponseFactory.InvalidScope,
                _ => ErrorResponseFactory.BadRequest
            },
            ErrorDescription = $"{code}: {oauth2Error.Groups["Message"].Value}",
            TraceId = oauth2Error.Groups["TraceId"].Value,
            CorrelationId = oauth2Error.Groups["CorrelationId"].Value,
            Timestamp = oauth2Error.Groups["Timestamp"].Value
        };
        return new(true, response);
    }

    /// <summary>
    /// Regex that matches an az cli error message from an OAuth2 failure e.g. invalid scope.
    /// </summary>
    [GeneratedRegex(@"ERROR: (?<Code>\w+):\W+(?<Message>.+)\W+Trace ID: (?<TraceId>.+) Correlation ID: (?<CorrelationId>.+) Timestamp: (?<Timestamp>.+)$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex OAuth2ErrorRegex();

    private static ApiError<OAuth2ErrorResponse>? InteractiveAuthenticationRequired(Exception ex)
    {
        var interactiveError = InteractiveErrorRegex().Match(ex.Message);
        if (!interactiveError.Success) return null;

        var command = interactiveError.Groups["Command"].Value;
        var response = new OAuth2ErrorResponse()
        {
            Error = "interactive_authentication_required",
            ErrorDescription = $"Interactive authentication required. Please run {command}"
        };
        return new(false, response);
    }

    /// <summary>
    /// Regex that matches the az cli error message indicating interactive authentication is required.
    /// </summary>
    [GeneratedRegex(
        @"Interactive authentication is needed\. Please run:\W+(?<Command>az login --scope [^ ]+)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex InteractiveErrorRegex();
}

public record ApiError<T>(bool BadRequest, T Response);