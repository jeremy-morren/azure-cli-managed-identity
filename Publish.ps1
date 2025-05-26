
$gitRev = git -C $PSScriptRoot describe --long --always --exclude=* --abbrev=12
$gitUrl = git -C $PSScriptRoot remote get-url origin

$latest = 'jeremysv/azcli-managed-identity:latest'
$tag = "jeremysv/azcli-managed-identity:${gitRev}"

try {
    Push-Location -Path $PSScriptRoot -StackName "Build"

    docker build --progress=plain `
        -t $latest -t $tag `
        --label "org.opencontainers.image.revision=$gitRev" `
        --label "org.opencontainers.image.source=$gitUrl" `
        -f "src/Container/Dockerfile" "src"
    docker push $tag
    docker push $latest
    docker pushrm $latest -s "Managed identity endpoint using Azure CLI for local development/CI"
}
finally {
    Pop-Location -StackName "Build"
}