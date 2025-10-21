param (
    [Parameter(Mandatory)][string]$OutputPath
)

function GetDirectorySize {
    param (
        [string]$Path
    )
    $size = 0
    Get-ChildItem -Recurse -File -Path $Path | ForEach-Object { $size += $_.Length }
    return $size
}

$zipFile = "AzureCLI.zip"
Write-Host "Downloading Azure CLI..."
Invoke-WebRequest -Uri 'https://aka.ms/installazurecliwindowszipx64' -OutFile $zipFile -UseBasicParsing

Write-Host "Expanding Azure CLI to $OutputPath"
Expand-Archive -Path $zipFile -DestinationPath $OutputPath -Force
Remove-Item $zipFile

Write-Host "Trimming Azure CLI installation..."

# We will only use az get-access-token
# Remove large unnecessary files to installation size

$sitePackages = Join-Path $OutputPath 'Lib\site-packages'

$largeSize = 500 * 1000 # 500 KB
Get-ChildItem -Directory (Join-Path $sitePackages "azure/cli/command_modules") `
    | Where-Object { (GetDirectorySize -Path $_.FullName) -gt $largeSize } `
    | Remove-Item -Recurse -Force

Get-ChildItem -Directory (Join-Path $sitePackages "azure/mgmt/resource/policy") | Remove-Item -Recurse -Force
Get-ChildItem -Directory (Join-Path $sitePackages "azure/mgmt/resource/resources") | Remove-Item -Recurse -Force

Write-Host "Done."
Get-ChildItem $OutputPath