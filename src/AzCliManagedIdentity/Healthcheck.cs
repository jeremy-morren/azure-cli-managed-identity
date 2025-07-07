using System.Diagnostics;
using System.Net;
using System.Text;

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable MethodHasAsyncOverloadWithCancellation

namespace AzCliManagedIdentity;

public static class Healthcheck
{
    public static async Task Handle(HttpContext context)
    {
        var ct = context.RequestAborted;
        var (statusCode, message) = await CheckHealth(ct);
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(message, ct);
    }

    private static async Task<(int, string)> CheckHealth(CancellationToken ct)
    {
        var (fileName, arguments) = GetCommand();
        // Check that az login --help succeeds
        var psi = new ProcessStartInfo()
        {
            FileName = fileName,
            UseShellExecute = false,
            ErrorDialog = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        foreach (var a in arguments)
            psi.ArgumentList.Add(a);
        using var process = Process.Start(psi);
        if (process == null)
            throw new Exception("Failed to start az login process.");
        await process.WaitForExitAsync(ct);
        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();

        if (process.ExitCode == 0)
            return (StatusCodes.Status200OK, "az login --help succeeded.");

        var body = new StringBuilder();
        body.AppendLine($"az login --help failed with exit code {process.ExitCode}");

        if (stdOut.Length > 0)
            body.AppendLine(stdOut);
        if (stdErr.Length > 0)
            body.AppendLine(stdErr);
        return (StatusCodes.Status503ServiceUnavailable, body.ToString());
    }

    private static (string FileName, string[] Arguments) GetCommand()
    {
        if (OperatingSystem.IsWindows())
            return ("cmd.exe", ["/D", "/Q", "/C", "az login --help"]);
        if (OperatingSystem.IsLinux())
            return ("/bin/sh", ["-c", "az login --help"]);
        throw new PlatformNotSupportedException();
    }
}