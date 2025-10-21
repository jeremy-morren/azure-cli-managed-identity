using System.Diagnostics;
using System.Text;

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable MethodHasAsyncOverloadWithCancellation

namespace AzCliManagedIdentity.Api;

public class CliHealthcheck
{
    private readonly string _command;

    public CliHealthcheck(string command) => _command = command;

    public async Task Handle(HttpContext context)
    {
        var ct = context.RequestAborted;
        var (statusCode, message) = await CheckHealth(ct);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync(message, ct);
    }

    public async Task<(int StatusCode, string Output)> CheckHealth(CancellationToken ct)
    {
        var (fileName, arguments) = GetCommand();
        // Check that executing the command works
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
            throw new Exception($"Failed to start process. Command: '{_command}'.");
        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        await process.WaitForExitAsync(ct);

        if (process.ExitCode == 0)
            return (StatusCodes.Status200OK, $"{_command} exited with code {process.ExitCode}");

        var body = new StringBuilder();
        body.AppendLine($"'{_command}' failed with exit code {process.ExitCode}");

        if (stdOut.Length > 0)
            body.AppendLine(stdOut);
        if (stdErr.Length > 0)
            body.AppendLine(stdErr);
        return (StatusCodes.Status503ServiceUnavailable, body.ToString());
    }

    private (string FileName, string[] Arguments) GetCommand()
    {
        if (OperatingSystem.IsWindows())
            return (@"C:\Windows\System32\cmd.exe", ["/D", "/Q", "/C", _command]);
        if (OperatingSystem.IsLinux())
            return ("/bin/sh", ["-c", _command]);
        throw new PlatformNotSupportedException();
    }
}