#FROM mcr.microsoft.com/dotnet/runtime-deps:8.0 AS base
#
## Install trimmed azure cli
#RUN --mount=type=bind,source=Container/InstallAzureCliTrimmed.sh,target=/InstallAzureCliTrimmed.sh /InstallAzureCliTrimmed.sh
#
## Add az cli virtual env to path
#ENV PATH="/opt/azcli/bin:$PATH"
#
## Setup environment variables used by the CGI application
## DEFAULT_AZURE_CONFIG is the location with files created by the Azure CLI run in the container (az --version)
## SOURCE_AZURE_CONFIG is the location of the Azure CLI config files mounted from the host
#ENV SOURCE_AZURE_CONFIG="/azureCli"
#
## The az cli token request timeout in seconds
#ENV AZURE_CLI_TIMEOUT="30"
#
#FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#RUN apt-get update && \
#    apt-get install -y \
#      clang zlib1g-dev \
#    && rm -rf /var/lib/apt/lists/*
#WORKDIR /src
#COPY "AzCliManagedIdentity/AzCliManagedIdentity.csproj" .
#RUN dotnet restore
#COPY "AzCliManagedIdentity/" ./
#RUN dotnet publish -c Release -o /app/publish
#
#FROM base AS final
#WORKDIR /app
#COPY --from=build /app/publish .
#EXPOSE 8080
#ENTRYPOINT ["/app/AzCliManagedIdentity"]
#
## Healthcheck: invoke health endpoint
#HEALTHCHECK --interval=60s --timeout=3s --retries=3 --start-period=3s --start-interval=5s \
#    CMD ["curl", "-sSf", "http://127.0.0.1:8080/healthz" ]

ARG WINDOWS_VERSION=ltsc2022
FROM mcr.microsoft.com/windows/servercore:$WINDOWS_VERSION as download

RUN powershell -NoLogo -Command \
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; \
    Invoke-WebRequest -Uri 'https://aka.ms/installazurecliwindowszipx64' -UseBasicParsing -OutFile 'C:\AzureCLI.zip'

WORKDIR /app
ADD Container/InstallAzureCliTrimmed.ps1 .
RUN powershell -NoLogo -File InstallAzureCliTrimmed.ps1 -ZipFile C:\AzureCLI.zip -OutputPath C:\AzureCLI

FROM mcr.microsoft.com/windows/nanoserver:$WINDOWS_VERSION as final

# Configure ASP.NET Core
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=download C:/AzureCLI C:/AzureCLI
EXPOSE 8080

RUN setx PATH "%PATH%;C:\AzureCLI\bin"