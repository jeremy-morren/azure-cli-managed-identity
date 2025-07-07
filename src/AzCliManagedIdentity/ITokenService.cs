namespace AzCliManagedIdentity;

public interface ITokenService
{
    Task<TokenResponse> RequestAzureCliToken(TokenRequest request, CancellationToken ct);
}