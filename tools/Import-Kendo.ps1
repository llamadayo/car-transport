[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][string]$PackageRoot,
 [string]$MvcDll,
 [string]$JQueryFile
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$source = (Resolve-Path $PackageRoot).Path
if (-not $MvcDll) {
 $candidateDlls = @(Get-ChildItem $source -Filter Kendo.Mvc.dll -Recurse | Where-Object { [Reflection.AssemblyName]::GetAssemblyName($_.FullName).Version.ToString() -eq '2019.1.115.545' })
 if ($candidateDlls.Count -ne 1) { throw 'Specify -MvcDll with the company MVC 5 DLL whose assembly version is exactly 2019.1.115.545.' }
 $MvcDll = $candidateDlls[0].FullName
}
if ([Reflection.AssemblyName]::GetAssemblyName((Resolve-Path $MvcDll).Path).Version.ToString() -ne '2019.1.115.545') { throw 'Kendo.Mvc.dll must be assembly version 2019.1.115.545.' }
function Find-One([string]$Name) {
 $files = @(Get-ChildItem $source -Recurse -File -Filter $Name)
 if ($files.Count -ne 1) { throw "Expected exactly one $Name under PackageRoot. Point PackageRoot at the extracted 2019.1.115 release folder." }
 return $files[0].FullName
}
$all = Find-One 'kendo.all.min.js'
$mvc = Find-One 'kendo.aspnetmvc.min.js'
if ((Get-Content $all -Raw) -notmatch '2019\.1\.115' -or (Get-Content $mvc -Raw) -notmatch '2019\.1\.115') { throw 'Scripts must be Kendo 2019.1.115 from the same release.' }
$common = Find-One 'kendo.common.min.css'
$theme = Find-One 'kendo.default.min.css'
foreach($file in @($common,$theme)) { if ((Get-Content $file -Raw) -notmatch '2019\.1\.115') { throw "Cannot verify stylesheet release: $file. Use the unmodified licensed distribution." } }
$culture = Find-One 'kendo.culture.zh-TW.min.js'
$messages = Find-One 'kendo.messages.zh-TW.min.js'
if (-not $JQueryFile) { $JQueryFile = Find-One 'jquery.min.js' }
if ((Get-Content $JQueryFile -Raw) -notmatch 'jQuery v3\.3\.1') { throw 'Use jQuery 3.3.1 (full build), passed with -JQueryFile if necessary.' }
$js = Join-Path $repo 'src/VD.Web/Scripts/kendo'
$css = Join-Path $repo 'src/VD.Web/Content/kendo'
$dll = Join-Path $repo 'vendor/kendo'
foreach ($folder in @($js,$css,$dll,(Join-Path $js 'cultures'),(Join-Path $js 'messages'))) { New-Item -ItemType Directory -Force -Path $folder | Out-Null }
Copy-Item $MvcDll (Join-Path $dll 'Kendo.Mvc.dll') -Force
Copy-Item $all (Join-Path $js 'kendo.all.min.js') -Force
Copy-Item $mvc (Join-Path $js 'kendo.aspnetmvc.min.js') -Force
Copy-Item $JQueryFile (Join-Path $js 'jquery.min.js') -Force
Copy-Item $culture (Join-Path $js 'cultures/kendo.culture.zh-TW.min.js') -Force
Copy-Item $messages (Join-Path $js 'messages/kendo.messages.zh-TW.min.js') -Force
# Keep theme sprites/fonts and their relative paths, not just two CSS files.
Copy-Item (Join-Path (Split-Path $common) '*') $css -Recurse -Force
Copy-Item $theme (Join-Path $css 'kendo.default.min.css') -Force
Write-Host 'Imported licensed MVC DLL 2019.1.115.545, Kendo assets 2019.1.115, and jQuery 3.3.1. These paths are excluded from Git.'
