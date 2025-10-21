using AzCliManagedIdentity.ManagedIdentity;

namespace AzCliManagedIdentity;

public interface ITokenService
{
    Task<MsiTokenResponse> GetAccessToken(TokenRequest request, CancellationToken ct);
}