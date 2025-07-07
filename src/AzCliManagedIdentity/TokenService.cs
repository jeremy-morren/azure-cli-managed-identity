using Azure.Identity;
using ILogger = Serilog.ILogger;

// ReSharper disable MethodHasAsyncOverload

namespace AzCliManagedIdentity;

public class TokenService : ITokenService
{
    private readonly ILogger _logger;

    public TokenService(ILogger logger)
    {
        _logger = logger.ForContext<TokenService>();
    }

    private static readonly SemaphoreSlim Mutex = new(1, 1);

    /// <summary>
    /// Requests a token using the Azure CLI credential.
    /// </summary>
    /// <remarks>
    /// This method cannot be run in parallel (due to the environment variable)
    /// Shouldn't be a problem for a CGI process
    /// </remarks>
    public async Task<TokenResponse> RequestAzureCliToken(TokenRequest request, CancellationToken ct)
    {
        LogRequest(request);

        // First copy the Azure CLI config files to a temp directory
        using var temp = new TempDirectory();
        SetupAzureCliFiles.CopyFilesForRequest(temp.Path);
        WriteWarnings(temp.Path);

        // Because we are setting an environment variable, synchronization is required
        await Mutex.WaitAsync(ct);
        try
        {
            // Set the environment variable to point to the temp directory
            Environment.SetEnvironmentVariable("AZURE_CONFIG_DIR", temp.Path, EnvironmentVariableTarget.Process);

            var options = new AzureCliCredentialOptions()
            {
                ProcessTimeout = GetTimeout()
            };
            var credential = new AzureCliCredential(options);
            var token = await credential.GetTokenAsync(request.CreateTokenRequestContext(), ct);
            var response = new TokenResponse(token);
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
        var timeout = Environment.GetEnvironmentVariable("AZURE_CLI_TIMEOUT");
        if (double.TryParse(timeout, out var parsedTimeout))
            return TimeSpan.FromSeconds(parsedTimeout);
        return null;
    }

    #region Logging

    private void LogRequest(TokenRequest request)
    {
        _logger.Information("Requesting token with Azure CLI credential. Resource: {Resource}", request.Resource);
    }

    /// <summary>
    /// Writes a log message to stderr about the acquired token.
    /// </summary>
    private void LogResponse(TokenResponse response)
    {
        var notBefore = DateTimeOffset.FromUnixTimeSeconds(response.NotBefore);
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(response.ExpiresOn);
        var validFor = expiresAt - notBefore;
        _logger.Information("Acquired token using Azure CLI credential. Resource: {Resource}. Valid for: {ValidFor:g}. Expires At: {ExpiresAt:yyyy-MM-dd HH:mm:ss zzz}",
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
            Console.WriteLine("""
                              Warning: az.json not found. Ensure that the Azure CLI config directory is mounted correctly.
                              Example docker-compose.yml:
                              services:
                                managed-identity:
                                  image: jeremysv/azcli-managed-identity
                                  volumes:
                                    - "{USERPROFILE:-~}/.azure:/azureCli:ro"
                              """);
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