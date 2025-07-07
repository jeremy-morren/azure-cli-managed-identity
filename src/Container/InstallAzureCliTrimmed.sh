#!/bin/sh

set -e
apt-get update
apt-get install -y --no-install-recommends \
    curl \
    jq \
    apt-transport-https \
    ca-certificates \
    python3-minimal \
    python3-pip \
    python3-venv

# Use /opt/azcli as the location for the Azure CLI
VENV='/opt/azcli'

python3 -m venv "$VENV"

#See https://github.com/Azure/azure-cli/issues/27755#issuecomment-1883989904

"$VENV/bin/pip" install --no-deps \
    azure-cli \
    azure-cli-core \
    azure-cli-telemetry \
    azure-common \
    azure-core \
    azure-mgmt-core \
    azure-mgmt-resource \
    azure-mgmt-subscription \
    anyio \
    argcomplete \
    certifi \
    chardet \
    charset-normalizer \
    humanfriendly \
    idna \
    isodate \
    jmespath \
    knack \
    msal \
    msal-extensions \
    msrest \
    msrestazure \
    oauthlib \
    packaging \
    pkginfo \
    portalocker \
    PyYAML \
    requests \
    requests-oauthlib \
    setuptools \
    six \
    sniffio \
    typing_extensions \
    urllib3

# Remove unnecessary azure CLI files to reduce image size
rm -r $VENV/lib/python3.*/site-packages/azure/mgmt/resource/policy/v*
rm -r $VENV/lib/python3.*/site-packages/azure/mgmt/resource/resources/v*
du -s -h -t 1M $VENV/lib/python3.*/site-packages/azure/cli/command_modules/* | awk '{print $2}' | xargs rm -r

# Uninstall pip and venv to reduce image size
apt-get remove -y --purge \
    python3-pip \
    python3-venv

# Clean up apt cache
rm -rf /var/lib/apt/lists/*

# On first runs, the Azure CLI will discover commands etc. Subsequent runs will be faster.
/opt/azcli/bin/az --version
/opt/azcli/bin/az login --help > /dev/null # First run setup
