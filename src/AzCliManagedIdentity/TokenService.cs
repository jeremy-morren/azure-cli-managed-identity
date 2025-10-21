using AzCliManagedIdentity.ManagedIdentity;
using Azure.Core;
using Azure.Identity;
using ILogger = Serilog.ILogger;

// ReSharper disable MethodHasAsyncOverload

namespace AzCliManagedIdentity;

public class TokenService : ITokenService
{
    private readonly ILogger _logger;
    private readonly TokenCredential _credential;

    public TokenService(ILogger logger)
    {
        _logger = logger.ForContext<TokenService>();

        _credential = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions()
            {
                ExcludeInteractiveBrowserCredential = true,
                CredentialProcessTimeout = GetTimeout()
            });
    }

    public async Task<MsiTokenResponse> GetAccessToken(TokenRequest request, CancellationToken ct)
    {
        // Try to request the token directly first
        var response = await TryGetToken(request, ct);
        if (response != null)
            return response;

        // If that fails, re-copy the Azure CLI config files and try again
        return await RequestTokenWithCopy(request, ct);
    }
    
    /// <summary>
    /// Tries to request the token directly without copying config files.
    /// </summary>
    private async Task<MsiTokenResponse?> TryGetToken(TokenRequest request, CancellationToken ct)
    {
        try
        {
            var token = await _credential.GetTokenAsync(request.CreateTokenRequestContext(), ct);
            var response = new MsiTokenResponse(token);
            LogResponse(response);
            return response;
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Direct token request failed. Falling back to token request with copied Azure CLI config files");
            return null;
        }
    }

    /// <summary>
    /// Synchronization mutex for setting the AZURE_CONFIG_DIR environment variable.
    /// </summary>
    private static readonly SemaphoreSlim Mutex = new(1, 1);

    /// <summary>
    /// Copies the Azure CLI config files to a temp directory and requests a token.
    /// </summary>
    /// <remarks>
    /// This method cannot be run in parallel (due to the environment variable)
    /// </remarks>
    private async Task<MsiTokenResponse> RequestTokenWithCopy(TokenRequest request, CancellationToken ct)
    {
        // First copy the Azure CLI config files to a temp directory
        using var temp = new TempDirectory();
        SetupAzureCliFiles.CopyFilesForRequest(temp.Path);
        WriteWarnings(temp.Path);

        await Mutex.WaitAsync(ct);
        try
        {
            // Set the environment variable to point to the temp directory
            Environment.SetEnvironmentVariable("AZURE_CONFIG_DIR", temp.Path, EnvironmentVariableTarget.Process);

            var token = await _credential.GetTokenAsync(request.CreateTokenRequestContext(), ct);
            var response = new MsiTokenResponse(token);
            LogResponse(response);
            return response;
        }
        finally
        {
            Mutex.Release();
        }
    }

    private static TimeSpan? GetTimeout()
    {
        var timeout = Environment.GetEnvironmentVariable("MSI_REQUEST_TIMEOUT");
        if (double.TryParse(timeout, out var parsedTimeout) && parsedTimeout > 0)
            return TimeSpan.FromSeconds(parsedTimeout);
        return null;
    }

    #region Logging

    /// <summary>
    /// Writes a log message to stderr about the acquired token.
    /// </summary>
    private void LogResponse(MsiTokenResponse response)
    {
        var notBefore = DateTimeOffset.FromUnixTimeSeconds(response.NotBefore);
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(response.ExpiresOn);
        var validFor = expiresAt - notBefore;
        _logger.Information("Acquired access token. Resource: {Resource}. Valid for: {ValidFor:g}. Expires At: {ExpiresAt:yyyy-MM-dd HH:mm:ss zzz}",
            response.Resource, validFor, expiresAt);
    }

    #endregion

    /// <summary>
    /// Writes warnings to stderr if the Azure CLI config directory is not mounted correctly.
    /// </summary>
    /// <param name="tempDir"></param>
    private static void WriteWarnings(string tempDir)
    {
        // Check for az.json file. If not present, print warning about incorrect volume mount
        var azJsonFile = Path.Combine(tempDir, "az.json");
        if (!File.Exists(azJsonFile))
        {
            if (OperatingSystem.IsWindows())
            {
                Console.WriteLine("""
                                Warning: az.json not found. Ensure that the Azure CLI config directory is mounted correctly.
                                Example docker-compose.yml:
                                services:
                                  managed-identity:
                                    image: ghcr.io/jeremy-morren/azure-cli-managed-identity:latest
                                    volumes:
                                      - "${AZURE_CONFIG_DIR:-${USERPROFILE:-~}/.azure}:C:/.azure:ro"
                                """);
            }
            else
            {
                Console.WriteLine("""
                                Warning: az.json not found. Ensure that the Azure CLI config directory is mounted correctly.
                                Example docker-compose.yml:
                                services:
                                  managed-identity:
                                    image: ghcr.io/jeremy-morren/azure-cli-managed-identity:latest
                                    volumes:
                                      - "${AZURE_CONFIG_DIR:-${USERPROFILE:-~}/.azure}:/.azure:ro"
                                """);
            }
            return;
        }

        // Check for the msal_token_cache.json file
        var msalTokenCacheFileJson = Path.Combine(tempDir, "msal_token_cache.json");
        if (File.Exists(msalTokenCacheFileJson))
            return;

        // Check to see if the msal_token_cache.json file is encrypted (i.e. msal_token_cache.bin exists)
        var msalTokenCacheFileBin = Path.Combine(SetupAzureCliFiles.GetSourceConfigDir(), "msal_token_cache.bin");
        Console.WriteLine(
            File.Exists(msalTokenCacheFileBin)
                ? """
                  Token encryption is enabled enabled (default on Windows).
                  This will cause all token requests to fail. To fix this error, run the following command:
                  az config set core.encrypt_token_cache=false
                  See https://github.com/Azure/azure-cli/issues/29193#issuecomment-2174836155
                  """
                : "Warning: msal_token_cache.json not found. Ensure that the Azure CLI config directory is mounted correctly");
    }

}