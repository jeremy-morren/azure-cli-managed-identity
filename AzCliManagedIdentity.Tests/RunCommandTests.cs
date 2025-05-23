using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Shouldly;
using Xunit.Abstractions;

namespace AzCliManagedIdentity.Tests;

/// <summary>
/// MSI endpoint is at <see cref="Microsoft.Identity.Client.ManagedIdentity.EnvironmentVariables.MsiEndpoint"/>.
/// See <see cref=" Microsoft.Identity.Client.ManagedIdentity.ManagedIdentityClient.GetManagedIdentitySource"/>
/// </summary>
public class RunCommandTests(ITestOutputHelper output)
{
    [Fact]
    public void TestEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("AZURE_CONFIG_DIR", "/home/user/.azure");

        var processStartInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe")),
            ArgumentList = { "/C", "SET" },
            UseShellExecute = false,
            ErrorDialog = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var process = Process.Start(processStartInfo);
        process.ShouldNotBeNull();
        process.WaitForExit();
        process.ExitCode.ShouldBe(0);
        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        output.WriteLine(stdOut);
    }
}