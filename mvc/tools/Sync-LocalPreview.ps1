param([Parameter(Mandatory=$true)][string]$PreviewPath)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$preview=(Resolve-Path -LiteralPath $PreviewPath).Path
if (!$preview.StartsWith((Join-Path $root 'artifacts')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or !(Test-Path -LiteralPath (Join-Path $preview 'applicationhost.config'))) { throw 'Only an existing workspace IIS preview may be updated.' }
$offline=Join-Path $preview 'app_offline.htm'
if(Test-Path -LiteralPath $offline){throw 'Preview already offline; leaving existing maintenance state unchanged.'}
$bin=Join-Path $root 'Web\bin\LocalDemo'
foreach($dll in @('CARGO.Web.dll','CARGO.Service.dll','CARGO.DTO.dll')) { if(!(Test-Path -LiteralPath (Join-Path $bin $dll))){throw "Missing $dll; build LocalDemo first."} }
[IO.File]::WriteAllText($offline,'Local preview is being updated.')
# Keep the app offline on failure instead of serving a mixed assembly set.
Get-ChildItem -LiteralPath $bin -File | Copy-Item -Destination (Join-Path $preview 'bin') -Force
foreach($name in @('Scripts','Content')) { Copy-Item -LiteralPath (Join-Path $root ('Web\'+$name)) -Destination $preview -Recurse -Force }
$views=Join-Path $root 'Web\Views'
Get-ChildItem -LiteralPath $views -Recurse -Filter '*.cshtml' | ForEach-Object {
 $relative=$_.FullName.Substring($views.Length+1)
 $destination=Join-Path (Join-Path $preview 'Views') $relative
 New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
 Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
}
Get-ChildItem -LiteralPath $bin -Filter '*.dll' | ForEach-Object {
 if((Get-FileHash -LiteralPath $_.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path (Join-Path $preview 'bin') $_.Name)).Hash) { throw "DLL verification failed: $($_.Name)" }
}
& (Join-Path $PSScriptRoot 'Test-PreviewRuntime.ps1') -PreviewPath $preview
Remove-Item -LiteralPath $offline
Write-Output 'All preview DLLs, views and static assets synchronized; configuration preserved.'
