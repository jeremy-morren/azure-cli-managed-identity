using System.Net;

namespace AzCliManagedIdentity;

public static class CgiResponse
{
    /// <summary>
    /// Writes a CGI response.
    /// </summary>
    public static void WriteResponse(
        HttpStatusCode statusCode,
        string? contentType = null,
        Dictionary<string, string>? headers = null,
        string? body = null)
    {
        Console.Out.WriteLine($"Status: {(int)statusCode}");
        if (contentType != null)
            Console.Out.WriteLine($"Content-Type: {contentType}");
        if (headers != null)
            foreach (var (key, value) in headers)
                Console.Out.WriteLine($"{key}: {value}");
        Console.Out.WriteLine();
        if (body != null)
            Console.Out.WriteLine(body);
    }

    /// <summary>
    /// Writes a CGI error response.
    /// </summary>
    public static void WriteError(string error)
    {
        // Write to standard error to show in HTTP server logs
        Console.Error.WriteLine("Managed identity error:");
        Console.Error.WriteLine(error);

        // Write to standard out to send to client
        Console.Out.WriteLine("Status: 500");
        Console.Out.WriteLine("Content-Type: text/plain");
        Console.Out.WriteLine();
        Console.Out.WriteLine(error);
    }
}