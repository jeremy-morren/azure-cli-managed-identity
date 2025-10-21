using System.Net;
using Serilog;
using Serilog.Events;

namespace AzCliManagedIdentity.Api;

public static class LoggingHelpers
{
    public static void UseAppRequestLogging(this IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging(static o =>
        {
            o.MessageTemplate =
                "{RemoteAddress} {RequestMethod} {RequestPath} {StatusCode} {Elapsed:0.00}ms {UserAgent}";
            o.IncludeQueryInRequestPath = true;

            o.EnrichDiagnosticContext = static (diagnosticContext, httpContext) =>
            {
                var remoteEndpoint = GetRemoteEndpoint(httpContext);
                diagnosticContext.Set("RemoteAddress", remoteEndpoint ?? string.Empty);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            };

            o.GetLevel = static (context, elapsed, ex) =>
            {
                if (ex != null || context.Response.StatusCode >= 500)
                    return LogEventLevel.Error;
                if (elapsed > 2500)
                    return LogEventLevel.Warning;
                if (context.Request.Path.StartsWithSegments("/healthz"))
                    return LogEventLevel.Debug;
                return LogEventLevel.Information;
            };
        });
    }

    public static string? GetRemoteEndpoint(HttpContext context) =>
        GetRemoteEndpoint(context.Connection.RemoteIpAddress);

    public static string? GetRemoteEndpoint(IPAddress? address)
    {
        if (address == null)
            return null;
        if (IPAddress.IsLoopback(address))
            return address.ToString(); // For loopback, just return the IP
        try
        {
            var hostEntry = Dns.GetHostEntry(address);
            return string.IsNullOrEmpty(hostEntry.HostName) ? address.ToString() : hostEntry.HostName;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to get remote endpoint. Address: {Address}", address.ToString());
            return address.ToString();
        }
    }
}