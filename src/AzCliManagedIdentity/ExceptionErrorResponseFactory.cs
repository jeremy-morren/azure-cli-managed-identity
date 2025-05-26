using System.Text.RegularExpressions;
using Azure.Identity;

namespace AzCliManagedIdentity;

public static partial class ExceptionErrorResponseFactory
{
    public static ErrorResponse FromException(AuthenticationFailedException ex)
    {
        return new ErrorResponse()
        {
            Error = new ErrorMessage("invalid_request", GetErrorMessage(ex))
        };
    }

    private static string GetErrorMessage(AuthenticationFailedException ex)
    {
        var interactiveError = InteractiveErrorRegex().Match(ex.Message);
        if (interactiveError.Success)
        {
            var command = interactiveError.Groups["Command"].Value;
            return $"Interactive authentication required. Please run {command}";
        }

        return ex.Message;
    }
    
    [GeneratedRegex(
        @"Interactive authentication is needed\. Please run:\W+(?<Command>az login --scope [^ ]+)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    public static partial Regex InteractiveErrorRegex();
}