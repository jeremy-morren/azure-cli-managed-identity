param (
    [string]$ZipFile,
    [string]$OutputPath
)

function GetDirectorySize {
    param (
        [string]$Path
    )
    $size = 0
    Get-ChildItem -Recurse -File -Path $Path | ForEach-Object { $size += $_.Length }
    return $size
}

Expand-Archive -Path $ZipFile -DestinationPath $OutputPath -Force

# We will only use az get-access-token
# Remove large unnecessary files to installation size

$sitePackages = Join-Path $OutputPath 'Lib\site-packages'

$largeSize = 500 * 1000 # 500 KB
Get-ChildItem -Directory (Join-Path $sitePackages "azure/cli/command_modules") `
    | ? { (GetDirectorySize -Path $_.FullName) -gt $largeSize } `
    | Remove-Item -Recurse -Force

Get-ChildItem -Directory (Join-Path $sitePackages "azure/mgmt/resource/policy") | Remove-Item -Recurse -Force
Get-ChildItem -Directory (Join-Path $sitePackages "azure/mgmt/resource/resources") | Remove-Item -Recurse -Force
