using System.Diagnostics;
using System.Net;

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable MethodHasAsyncOverloadWithCancellation

namespace AzCliManagedIdentity;

public static class Healthcheck
{
    public static bool IsHealthcheck(CgiRequest request) =>
        request.Method == HttpMethod.Get && request.IsPath("/healthz");

    public static async Task Invoke(CancellationToken ct)
    {
        // Check that az login --help succeeds
        var psi = new ProcessStartInfo()
        {
            FileName = "/bin/sh",
            ArgumentList = { "-c", "az login --help" },
            UseShellExecute = false,
            ErrorDialog = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        using var process = Process.Start(psi);
        if (process == null)
            throw new Exception("Failed to start az login process.");
        await process.WaitForExitAsync(ct);
        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        if (process.ExitCode == 0)
        {
            CgiResponse.WriteResponse(HttpStatusCode.OK, body: "Healthy");
        }
        else
        {
            Console.Error.WriteLine("Healthcheck failed: az login --help did not succeed.");
            if (stdOut.Length > 0)
                Console.Error.WriteLine(stdOut);
            if (stdErr.Length > 0)
                Console.Error.WriteLine(stdErr);
            CgiResponse.WriteResponse(HttpStatusCode.ServiceUnavailable, body: "az login --help failed");
        }
    }
}