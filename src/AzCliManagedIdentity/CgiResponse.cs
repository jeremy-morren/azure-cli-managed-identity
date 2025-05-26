using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace AzCliManagedIdentity;

public static class CgiResponse
{
    /// <summary>
    /// Writes a CGI response.
    /// </summary>
    public static void WriteResponse(
        HttpStatusCode statusCode,
        string? contentType = null,
        string? body = null)
    {
        Console.Out.WriteLine($"Status: {(int)statusCode}");
        if (contentType != null)
            Console.Out.WriteLine($"Content-Type: {contentType}");
        Console.Out.WriteLine();
        if (body != null)
            Console.Out.Write(body);
    }
    
    /// <summary>
    /// Writes a JSON CGI response.
    /// </summary>
    public static void WriteJsonResponse<T>(
        HttpStatusCode statusCode,
        T value,
        JsonTypeInfo<T> jsonTypeInfo)
    {
        var json = JsonSerializer.Serialize(value, jsonTypeInfo);
        Console.Out.WriteLine($"Status: {(int)statusCode}");
        Console.Out.WriteLine("Content-Type: application/json");
        Console.Out.WriteLine();
        Console.Out.Write(json);
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
        Console.Out.Write(error);
    }
}