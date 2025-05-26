using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace AzCliManagedIdentity;

public class CgiRequest
{
    /// <summary>
    /// CGI request method
    /// </summary>
    public required HttpMethod Method { get; init; }

    /// <summary>
    /// CGI request URI
    /// </summary>
    public required Uri RequestUri { get; init; }

    /// <summary>
    /// Request content type
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// CGI request headers
    /// </summary>
    public required Dictionary<string, string?> Headers { get; init; }

    /// <summary>
    /// Body of the request
    /// </summary>
    public required byte[] Body { get; init; }

    /// <summary>
    /// Checks if the request URI matches the specified path.
    /// </summary>
    public bool IsPath(string path) => RequestUri.AbsolutePath.Equals(path, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the request from the environment and STDIN, or null if required environment variables are not set.
    /// </summary>
    public static CgiRequest? FromEnvironment()
    {
        var method = Environment.GetEnvironmentVariable("REQUEST_METHOD");
        var scheme = Environment.GetEnvironmentVariable("REQUEST_SCHEME");
        var host = Environment.GetEnvironmentVariable("HTTP_HOST");
        var relativePath = Environment.GetEnvironmentVariable("REQUEST_URI");
        if (string.IsNullOrEmpty(method)
            || string.IsNullOrEmpty(scheme)
            || string.IsNullOrEmpty(host)
            || string.IsNullOrEmpty(relativePath)
            || !Uri.TryCreate(relativePath, UriKind.Relative, out var relativeUri)
            || !Uri.TryCreate($"{scheme}://{host}", UriKind.Absolute, out var baseUri))
            return null;

        // Read the body from STDIN
        if (!int.TryParse(Environment.GetEnvironmentVariable("CONTENT_LENGTH"), out var contentLength)
            || contentLength < 0)
            return null;

        var body = new byte[contentLength];
        if (contentLength > 0)
        {
            using var stdIn = Console.OpenStandardInput();
            stdIn.ReadExactly(body);
        }

        return new CgiRequest()
        {
            Method = HttpMethod.Parse(method),
            RequestUri = new Uri(baseUri, relativeUri),
            Headers = GetCgiHeadersFromEnvironment(),
            ContentType = Environment.GetEnvironmentVariable("CONTENT_TYPE"),
            Body = body
        };
    }

    /// <summary>
    /// Gets the CGI headers.
    /// </summary>
    /// <returns></returns>
    private static Dictionary<string, string?> GetCgiHeadersFromEnvironment()
    {
        // Get headers prefixed with HTTP_ from environment variables
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var keyObj in Environment.GetEnvironmentVariables().Keys)
        {
            if (keyObj is not string key ||
                !key.StartsWith("HTTP_", StringComparison.OrdinalIgnoreCase)) continue;

            var headerName = key[5..].Replace('_', '-');
            var headerValue = Environment.GetEnvironmentVariable(key);
            result[headerName] = headerValue;
        }
        return result;
    }

    /// <summary>
    /// Shows the request in a human-readable format.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{Method} {RequestUri}");
        if (ContentType != null)
            sb.AppendLine($"Content-Type: {ContentType}");
        foreach (var (key, value) in Headers)
            sb.AppendLine($"{key}: {value}");
        sb.AppendLine();
        if (Body.Length > 0)
            sb.AppendLine(Encoding.UTF8.GetString(Body)); // Body ends with a new line
        return sb.ToString();
    }

    /// <summary>
    /// Reads the body as a form URL encoded request.
    /// </summary>
    public Dictionary<string, StringValues> ReadFormUrlEncodedBody()
    {
        using var ms = new MemoryStream(Body);
        using var reader = new FormReader(ms);
        return reader.ReadForm();
    }
}