# Managed identity endpoint using Azure CLI for local development/CI

Using azure services from a docker container is currently very painful. This project solves that by
providing a managed identity API in a docker container that uses azure cli credentials from the host machine.
All other services in a docker compose project can use [`ManagedIdentityCredential`](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.managedidentitycredential?view=azure-dotnet) (or language equivalent).

## Usage

On windows, azure cli encrypts the token cache, which means it is not usable by the docker container. To fix this, disable encryption: 

`az config set core.encrypt_token_cache=false`

See https://github.com/Azure/azure-cli/issues/29193#issuecomment-2174836155.

For all services that need to use the credential, set `MSI_ENDPOINT` environment variable to `http://{managed-identity-service}/token`

Example `docker-compose.yaml`

```yaml
services:
  # Linux container
  managed-identity:
    image: ghcr.io/jeremy-morren/azure-cli-managed-identity:latest
    volumes:
      # Mount the host azure config into the container (read-only)
      - "${AZURE_CONFIG_DIR:-${USERPROFILE:-~}/.azure}:/.azure:ro"
    ports:
      - '11430:80/tcp'
    cpu_count: 1
    mem_limit: 64m
    
  # Windows container
  managed-identity:
    image: ghcr.io/jeremy-morren/azure-cli-managed-identity:latest
    volumes:
      # Mount the host azure config into the container (read-only)
      - "${AZURE_CONFIG_DIR:-${USERPROFILE:-~}/.azure}:C:/.azure:ro"
    ports:
      - '11430:80/tcp'

  service:
    ... configure service
    environment:
      # Configure ManagedIdentityCredential endpoint
      MSI_ENDPOINT: 'http://managed-identity/token'
    depends_on:
      managed-identity:
        condition: service_healthy
```

### Other endpoints

In addition to the `/token` endpoint, the API also implements the following endpoints:
- IMDS endpoint: `/metadata/identity/oauth2/token` (see [Get a token using HTTP](https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/how-to-use-vm-token#get-a-token-using-http))
- OAuth2 token endpoint: `/oauth2/token` - implements an OAuth2 endpoint that requires no credentials. 
  `curl "http://localhost:11430/oauth2/token" -H "Content-Type: application/x-www-form-urlencoded" --data-urlencode "scope=https://management.azure.com/.default"`

### Azure Pipelines

On azure pipelines, the service can be used for easily running code that needs to authenticate to Azure.

Example `azure-pipelines.yaml`:

```yaml
jobs:
  # NB: Assuming docker-compose.yaml file above is defined at project root

  - job: Linux
    pool:
      vmImage: ubuntu-latest
    steps:
      - task: AzureCLI@2
        inputs:
          azureSubscription: 'AzureServiceConnection'
          scriptType: bash
          scriptLocation: inlineScript
          inlineScript: docker compose up -d --build --wait
      
      # Example C#: 
      # new ManagedIdentityCredential().GetToken(new TokenRequestContext(["https://management.azure.com/.default"])))
      - script: dotnet run ...
        env:
          MSI_ENDPOINT: http://localhost:11430/token
  
  - job: Windows
    pool:
      vmImage: windows-latest
    variables:
      # Azure CLI Disable token encryption
      AZURE_CORE_ENCRYPT_TOKEN_CACHE: 'false'
    steps:
      - task: AzureCLI@2
        inputs:
          azureSubscription: 'AzureServiceConnection'
          scriptType: batch
          scriptLocation: inlineScript
          inlineScript: docker compose up -d --build --wait
      
      # Example C#: 
      # new ManagedIdentityCredential().GetToken(new TokenRequestContext(["https://management.azure.com/.default"])))
      - script: dotnet run ...
        env:
          MSI_ENDPOINT: http://localhost:11430/token

```

### Jetbrains HTTP client authentication

JetBrains IDEs provide a built-in HTTP client (see [HTTP Client | IntelliJ IDEA Documentation](https://www.jetbrains.com/help/idea/http-client-in-product-code-editor.html)). This project allows Azure AD authentication to be used without credentials via the `/oauth2/token` endpoint (see [OAuth 2.0 authorization | Intellij IDEA Documentation](https://www.jetbrains.com/help/idea/oauth-2-0-authorization.html)).

`AzureRequests.http`
```http
GET https://management.azure.com/subscriptions?api-version=2020-01-01
Authorization: Bearer {{$auth.token("AzureAD")}}
Accept: application/json
```

`http-client.env.json`
```json
{
  "Azure": {
    "Security": {
      "Auth": {
        "AzureAD": {
          "Type": "OAuth2",
          "Token URL": "http://localhost:11430/oauth2/token",
          "Scope": "https://management.azure.com/.default",

          "Grant Type": "Client Credentials",
          "Client ID": "",
          "Client Secret": ""
        }
      }
    }
  }
}
```


## Container image

Container image is hosted on Github at [Package azure-cli-managed-identity](https://github.com/jeremy-morren/azure-cli-managed-identity/pkgs/container/azure-cli-managed-identity).

`docker pull ghcr.io/jeremy-morren/azure-cli-managed-identity:latest`

Provided architectures are `linux/amd64`, `linux/arm64` and `windows/amd64`.

## Source

Source repository at https://github.com/jeremy-morren/azure-cli-managed-identity
