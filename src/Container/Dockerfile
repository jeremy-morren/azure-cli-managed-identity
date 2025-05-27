FROM mcr.microsoft.com/dotnet/runtime-deps:8.0 AS base

#Install lighttpd
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        lighttpd \
    && rm -rf /var/lib/apt/lists/*
EXPOSE 80/tcp

# Install trimmed azure cli
RUN --mount=type=bind,source=Container/InstallAzureCliTrimmed.sh,target=/InstallAzureCliTrimmed.sh /InstallAzureCliTrimmed.sh
    
# Add az cli virtual env to path
ENV PATH="/opt/azcli/bin:$PATH"

# Setup environment variables used by the CGI application
# DEFAULT_AZURE_CONFIG is the location with files created by the Azure CLI run in the container (az --version)
# SOURCE_AZURE_CONFIG is the location of the Azure CLI config files mounted from the host
ENV SOURCE_AZURE_CONFIG="/azureCli"

# The az cli token request timeout in seconds
ENV AZURE_CLI_TIMEOUT="30"

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
RUN apt-get update && apt-get install -y clang zlib1g-dev && rm -rf /var/lib/apt/lists/*
WORKDIR /src
COPY "AzCliManagedIdentity/AzCliManagedIdentity.csproj" ./
RUN dotnet restore
COPY "AzCliManagedIdentity/" ./
RUN dotnet publish -c Release -o /app/publish -p "PublishAot=true"

FROM base AS final
COPY --from=build /app/publish /app
COPY --chmod=111 Container/lighttpd.conf /app/lighttpd.conf
COPY --chmod=555 Container/Start.sh /app/Start.sh

ENTRYPOINT ["/bin/sh", "/app/Start.sh"]

# Healthcheck: invoke health endpoint of the CGI application. 
# Main use is on startup i.e. health shouldn't change after successful startup.
HEALTHCHECK --interval=60s --start-period=2s --start-interval=3s --timeout=2s --retries=3 \
    CMD ["curl", "-sSf", "http://127.0.0.1/healthz" ]