FROM mcr.microsoft.com/dotnet/runtime-deps:9.0 AS base

#Install lighttpd
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        lighttpd \
    && rm -rf /var/lib/apt/lists/*
EXPOSE 80/tcp

ENTRYPOINT ["/usr/sbin/lighttpd", "-D", "-f", "/etc/lighttpd/lighttpd.conf"]

#Install utilities & python3
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        curl \
        jq \
        apt-transport-https \
        ca-certificates \
        python3 \
        python3-pip \
        python3-venv \
    && rm -rf /var/lib/apt/lists/*

# Install trimmed azure cli
COPY Container/InstallAzureCliTrimmed.sh ./
RUN chmod +x InstallAzureCliTrimmed.sh && \
    ./InstallAzureCliTrimmed.sh && \
    rm -f ./InstallAzureCliTrimmed.sh
    
# Add az cli virtual env to path
ENV PATH="/opt/azcli:$PATH"


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
RUN apt-get update && apt-get install -y clang zlib1g-dev
WORKDIR /src
COPY "AzCliManagedIdentity/AzCliManagedIdentity.csproj" ./
RUN dotnet restore
COPY "AzCliManagedIdentity/" ./
RUN dotnet publish -c Release -o /app/publish -p "PublishAot=true"

FROM base AS final
COPY --from=build /app/publish /app
COPY --chmod=100 Container/lighttpd.conf /etc/lighttpd/lighttpd.conf
COPY --chmod=500 Container/CGI.sh /app/CGI.sh

# FROM base AS final
# COPY --chmod=100 Container/lighttpd.conf /etc/lighttpd/lighttpd.conf
