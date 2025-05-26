
try {
    Push-Location (Join-Path $PSScriptRoot "../..") -StackName "Build"

    $tag = 'test-cgi'
    docker build --progress=plain -t $tag -f "Container/Dockerfile" .

    if ($LASTEXITCODE -eq 0) {
        $azureCli = Join-Path ([Environment]::GetFolderPath("UserProfile")) ".azure"
        docker run --rm `
            -p 8080:80 `
            -v "${azureCli}:/azureCli:ro" `
            --name $tag $tag
    }
}
finally {
    Pop-Location -StackName "Build"
}