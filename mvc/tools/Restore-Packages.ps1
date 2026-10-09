param([switch]$LocalPreview)
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$destination = Join-Path $PSScriptRoot '..\packages'
$packages = @{
    'Microsoft.AspNet.Mvc' = '5.2.7'
    'Microsoft.AspNet.Razor' = '3.2.7'
    'Microsoft.AspNet.WebPages' = '3.2.7'
    'Microsoft.Web.Infrastructure' = '1.0.0.0'
    'Dapper' = '1.50.5'
    # Package 12.1.2400 contains assembly version 4.121.2.0.
    'Oracle.ManagedDataAccess' = '12.1.2400'
    'Newtonsoft.Json' = '9.0.1'
    'jQuery' = '3.7.1'
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
if ($LocalPreview) {
    $packages['Microsoft.AspNet.Mvc'] = '5.3.0'
    $packages['Microsoft.AspNet.Razor'] = '3.3.0'
    $packages['Microsoft.AspNet.WebPages'] = '3.3.0'
    $packages['Microsoft.Web.Infrastructure'] = '2.0.0'
    $packages['Newtonsoft.Json'] = '13.0.3'
    $packages['Microsoft.AspNet.WebApi.Core'] = '5.3.0'
    $packages['Telerik.SvgIcons'] = '4.11.0'
    $packages['Telerik.FontIcons'] = '4.11.0'
    $packages['Telerik.Licensing'] = '1.9.1'
}
foreach ($name in ($packages.Keys | Sort-Object)) {
    $version = $packages[$name]
    $target = Join-Path $destination "$name.$version"
    if (Test-Path -LiteralPath (Join-Path $target '.restored')) { continue }
    $normalized = $version -replace '^(\d+\.\d+\.\d+)\.0$', '$1'
    $id = $name.ToLowerInvariant()
    $url = "https://api.nuget.org/v3-flatcontainer/$id/$normalized/$id.$normalized.nupkg"
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    $archive = Join-Path $destination "$name.$version.zip"
    Invoke-WebRequest -Uri $url -OutFile $archive
    if (Test-Path -LiteralPath $target) { throw "Incomplete package directory exists: $target. Inspect it before retrying." }
    [IO.Compression.ZipFile]::ExtractToDirectory($archive, [IO.Path]::GetFullPath($target))
    Set-Content -LiteralPath (Join-Path $target '.restored') -Value $url
    Remove-Item -LiteralPath $archive
    Write-Output "Restored $name $version"
}
