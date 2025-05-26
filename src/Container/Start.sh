#!/bin/sh

if [ -z "$SOURCE_AZURE_CONFIG" ]; then
  echo "SOURCE_AZURE_CONFIG environment variable must be set"
  exit 1
fi

# Check to see if TF_BUILD is True. If it is, then we are running in Azure DevOps
# If we are, then we need to copy the azure cli config to a new location,
# since the credentials will be cleared by the AzureCli@2 task when the task ends
if [ "$TF_BUILD" = "True" ]; then
  echo "Running in Azure DevOps, copying Azure CLI config"
  cp -r "${SOURCE_AZURE_CONFIG}" /azureCliSaved
  export SOURCE_AZURE_CONFIG="/azureCliSaved"
fi

# Start the lighttpd server
exec /usr/sbin/lighttpd -D -f /app/lighttpd.conf