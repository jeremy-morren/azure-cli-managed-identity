#!/bin/sh

set -e

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

du -s -h -t 1M /opt/azcli/lib/python3.*/site-packages/azure/cli/command_modules/* | awk '{print $2}' | xargs rm -r
    rm -r /opt/azcli/lib/python3.*/site-packages/azure/mgmt/resource/policy/v*
    rm -r /opt/azcli/lib/python3.*/site-packages/azure/mgmt/resource/templatespecs/v*
    rm -r /opt/azcli/lib/python3.*/site-packages/azure/mgmt/resource/resources/v*