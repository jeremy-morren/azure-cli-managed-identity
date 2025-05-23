using Azure.Core;
using Azure.Identity;

namespace AzCliManagedIdentity;

public class TokenService
{
    private readonly string _azureCliConfig;

    public TokenService(string azureCliConfig)
    {
        _azureCliConfig = azureCliConfig;
    }

    public async Task<TokenResponse> RequestAzureCliToken(TokenRequest request, CancellationToken ct)
    {
        // NB: This method cannot be run in parallel (due to the environment variable)
        // Shouldn't be a problem for a CGI process

        // First copy the Azure CLI config files to a temp directory
        using var temp = new TempDirectory();
        foreach (var file in GetFilesToCopy(_azureCliConfig))
            File.Copy(file, Path.Combine(temp.Path, Path.GetFileName(file)));

        // Set the environment variable to point to the temp directory
        Environment.SetEnvironmentVariable("AZURE_CONFIG_DIR", temp.Path, EnvironmentVariableTarget.Process);

        var credential = new AzureCliCredential();
        var token = await credential.GetTokenAsync(request.CreateTokenRequestContext(), ct);
        return new TokenResponse(token);
    }

    public static IEnumerable<string> GetFilesToCopy(string azureCliConfig) =>
        FilesToCopy.SelectMany(pattern => Directory.GetFiles(azureCliConfig, pattern, SearchOption.TopDirectoryOnly));

    /// <summary>
    /// Wildcard file names to copy from the Azure CLI config directory. These are the files required to get a token.
    /// </summary>
    private static readonly string[] FilesToCopy =
    [
        "az.*",
        "azureProfile.json",
        "*config",
        "msal*"
    ];
}