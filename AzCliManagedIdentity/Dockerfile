FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base

# Install trimmed azure cli
COPY InstallAzureCliTrimmed.sh ./
RUN chmod +x InstallAzureCliTrimmed.sh && \
    ./InstallAzureCliTrimmed.sh && \
    rm -f ./InstallAzureCliTrimmed.sh

# Add az cli virtual env to path
ENV PATH="/opt/azcli/bin:$PATH"

#RUN apt-get update && \
#    apt-get install -y --no-install-recommends \
#        curl \
#    && curl -sL https://aka.ms/InstallAzureCLIDeb | bash \
#    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY "AzCliManagedIdentity.csproj" ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AzCliManagedIdentity.dll"]




