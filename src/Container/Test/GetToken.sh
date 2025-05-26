#!/bin/bash

set -e

echo "Starting"

az login --help > /dev/null #First run setup

tempDir="$(mktemp -d)"

export AZURE_CONFIG_DIR="$tempDir"

cp \
    ~/.azure/commandIndex.json \
    ~/.azure/versionCheck.json \
    "$tempDir"

# COPY files from /azureCli to tempDir
cp \
    /azureCli/az.* \
    /azureCli/azureProfile.json \
    /azureCli/*config \
    /azureCli/msal*.json \
    "$tempDir"
    
ls -laR "$tempDir"

auth="$(az account get-access-token --scope 'https://management.azure.com/.default' -o json | jq -r '(.tokenType + " " + .accessToken)')"
echo "Acquired access token"

subscription="$(curl -sSf -H "Authorization: $auth" -H 'Accept: application/json' -X GET 'https://management.azure.com/subscriptions?api-version=2021-04-01' | jq -r '.value[0].subscriptionId')"
echo "Using subscription: $subscription"

curl -sSf -H "Authorization: $auth" -H 'Accept: application/json' \
     -X GET "https://management.azure.com/subscriptions/${subscription}/resourceGroups?api-version=2021-04-01" \
     | jq -r '.value[] | .name'

# /app/DockerManagedIdentity $tempDir