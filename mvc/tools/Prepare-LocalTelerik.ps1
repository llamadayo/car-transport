param([string]$Installation = 'C:\Program Files (x86)\Progress\Telerik UI for ASP.NET MVC 2026 Q3')
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$dll = Join-Path $Installation 'wrappers\aspnetmvc\Binaries\Mvc5\Kendo.Mvc.dll'
if (!(Test-Path -LiteralPath $dll)) { throw 'Kendo MVC installation not found.' }
$version = [Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString()
if (!$version.StartsWith('2026.3.811.')) { throw "Unexpected Kendo version: $version. Review local dependencies first." }
$localLib = Join-Path $root 'lib\local'
$scripts = Join-Path $root 'Web\Scripts\vendor'
$styles = Join-Path $root 'Web\Content\vendor'
foreach ($directory in @($localLib, $scripts, $styles)) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
Copy-Item -LiteralPath $dll -Destination $localLib
foreach ($name in @('kendo.all.min.js', 'kendo.aspnetmvc.min.js')) { Copy-Item -LiteralPath (Join-Path $Installation "js\$name") -Destination $scripts }
Copy-Item -LiteralPath (Join-Path $Installation 'js\cultures\kendo.culture.zh-TW.min.js') -Destination $scripts
Copy-Item -LiteralPath (Join-Path $Installation 'js\messages\kendo.messages.zh-TW.min.js') -Destination $scripts
Copy-Item -LiteralPath (Join-Path $Installation 'styles\default-main.css') -Destination (Join-Path $styles 'kendo-theme.css')
Copy-Item -LiteralPath (Join-Path $root 'packages\jQuery.3.7.1\Content\Scripts\jquery-3.7.1.min.js') -Destination (Join-Path $scripts 'jquery.min.js')
Write-Output 'Local 2026 assets prepared in ignored folders. No license keys were accessed.'
