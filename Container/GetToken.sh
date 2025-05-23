#!/bin/bash

set -e

tempDir="$(mktemp -d)"

# COPY files from /azureCli to tempDir
cp \
    /azureCli/az.* \
    /azureCli/azureProfile.json \
    /azureCli/*config \
    /azureCli/msal_token* \
    "$tempDir"
    
ls -laR "$tempDir"

export AZURE_CONFIG_DIR="$tempDir"

auth="$(az account get-access-token --scope 'https://management.azure.com/.default' -o json | jq -r '(.tokenType + " " + .accessToken)')"

function getSubscription() {
    curl -sSf -H "Authorization: $auth" -H 'Accept: application/json' \
         -X GET 'https://management.azure.com/subscriptions?api-version=2021-04-01' \
         | jq -r '.value[0].subscriptionId'
}

subscription="$(getSubscription)"

curl -sSf -H "Authorization: $auth" -H 'Accept: application/json' \
     -X GET "https://management.azure.com/subscriptions/${subscription}/resourceGroups?api-version=2021-04-01" \
     | jq -r '.value[] | .name'

# /app/DockerManagedIdentity $tempDir