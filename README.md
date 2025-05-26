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
      - "${AZURE_CONFIG_DIR:-${USERPROFILE:-~}/.azure}:/azureCli:ro"
    environment:
      # Allow app to check whether it is running in Azure Pipelines
      - "TF_BUILD=${TF_BUILD:-False}"
    ports:
      - '50342:80/tcp'
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

### Azure Pipelines

On azure pipelines, the service can be used for easily running code that needs to authenticate to Azure.

Example `azure-pipelines.yaml`:

```yaml
pool:
  vmImage: ubuntu-latest
steps:
  #NB: Assuming docker-compose.yaml file above is defined at project root
  - task: AzureCLI@2
    inputs:
      azureSubscription: 'ConnectedServiceName'
      scriptType: pscore
      scriptLocation: inlineScript
      inlineScript: docker compose up -d --build --wait
  
  # Example C#: new ManagedIdentityCredential().GetToken(new TokenRequestContext(["https://management.azure.com/"])))
  - script: dotnet run ...
    env:
      MSI_ENDPOINT: http://localhost:50342/oauth2/token

```
### Source

Source repository at https://github.com/jeremy-morren/azure-cli-managed-identity