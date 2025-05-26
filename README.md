# Managed identity endpoint using Azure CLI for local development/CI

Using azure services from a docker container is currently very painful. This project solves that by
providing a managed identity API in a docker container that uses azure cli credentials from the host machine.
All other services in a docker compose project can use [`ManagedIdentityCredential`](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.managedidentitycredential?view=azure-dotnet) (or language equivalent).

## Usage

On windows, azure cli encrypts the token cache, which means it is not usable by the docker container. To fix this, disable encryption: 

`az config set core.encrypt_token_cache=false`

See https://github.com/Azure/azure-cli/issues/29193#issuecomment-2174836155.

For all services that need to use the credential, set `MSI_ENDPOINT` environment variable to `http://{managed-identity-service}/oauth2/token`

Example `docker-compose.yaml`

```yaml
services:
  managed-identity:
    image: jeremysv/azcli-managed-identity:latest
    volumes:
      #Mount the host azure config into the container (read-only)
      - "${USERPROFILE:-~}/.azure:/azureCli:ro"
    cpu_count: 1
    mem_limit: 32m

  service:
    ... configure service
    environment:
      #Configure ManagedIdentityCredential endpoint
      MSI_ENDPOINT: 'http://managed-identity/oauth2/token'
    depends_on:
      managed-identity:
        condition: service_healthy
```

### Source

Source repository at https://github.com/jeremy-morren/azure-cli-managed-identity