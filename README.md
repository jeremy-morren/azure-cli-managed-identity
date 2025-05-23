# Managed Identity service for developemtn

USing azure services from a docker container is currently very painful. This project solves that by
providing a managed identity API in a docker container that uses azure cli credentials from the host machine.
All other services in a docker compose project can use `ManagedIdentityCredential` (or language equivalent).

## Usage

On windows, azure cli encrypts the token cache, which means it is not usable by the docker container. To fix this, disable encryption: 

`az config set core.encrypt_token_cache=false`

See https://github.com/Azure/azure-cli/issues/29193#issuecomment-2174836155.

Example `docker-compose.yaml`

```yaml

services:
    image: jeremysv/managed-identity-docker:latest
    volumes:
        

```