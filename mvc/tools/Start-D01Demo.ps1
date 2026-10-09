param([ValidateRange(1024,65535)][int]$Port = 5188, [switch]$Background)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$iis = 'C:\Program Files\IIS Express\iisexpress.exe'
if (!(Test-Path -LiteralPath $iis)) { throw 'IIS Express is required. Install the Visual Studio ASP.NET web development tools first.' }
$vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = Get-Command msbuild -ErrorAction SilentlyContinue
if ($msbuild) { $builder = $msbuild.Source }
elseif (Test-Path -LiteralPath $vswhere) { $builder = & $vswhere -latest -products '*' -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1 }
if (!$builder) { throw 'MSBuild was not found. Open Developer PowerShell and retry.' }
# No downloads, database setup or license-key access. Preparation uses existing local assets.
& (Join-Path $PSScriptRoot 'Prepare-LocalTelerik.ps1')
& $builder (Join-Path $root 'CARGO.sln') /p:UseLocalTelerik=true /p:LocalDemo=true /p:UseSharedCompilation=false /t:Build /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw 'Local demo build failed. Check that the local preview packages have been restored.' }
$stage = & (Join-Path $PSScriptRoot 'Stage-LocalPreview.ps1') -Demo
$template = Join-Path (Split-Path $iis) 'AppServer\applicationhost.config'
[xml]$config = Get-Content -LiteralPath $template
$site = $config.configuration.'system.applicationHost'.sites.site
$site.SetAttribute('name', 'CargoD01Demo')
$site.application.SetAttribute('applicationPool', 'Clr4IntegratedAppPool')
$site.application.virtualDirectory.SetAttribute('physicalPath', $stage)
$site.bindings.binding.SetAttribute('bindingInformation', ":${Port}:localhost")
$logs = Join-Path $stage 'logs'
New-Item -ItemType Directory -Path $logs -Force | Out-Null
$defaults = $config.configuration.'system.applicationHost'.sites.siteDefaults
$defaults.logFile.SetAttribute('directory', $logs)
$defaults.traceFailedRequestsLogging.SetAttribute('directory', $logs)
$configFile = Join-Path $stage 'applicationhost.config'
$config.Save($configFile)
Write-Host "D01 demo: http://localhost:$Port/CARGO_D01"
Write-Host 'Fictional in-memory data only. Restarting the site resets changes.'
$arguments = @('/config:' + '"' + $configFile + '"', '/site:CargoD01Demo', '/systray:false', '/trace:error')
if ($Background) {
    $process = Start-Process -FilePath $iis -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logs 'iis.stdout.log') -RedirectStandardError (Join-Path $logs 'iis.stderr.log')
    Write-Output ([pscustomobject]@{ ProcessId = $process.Id; Url = "http://localhost:$Port/CARGO_D01"; Directory = $stage })
} else {
    Write-Host 'Keep this window open. Press Q to stop IIS Express.'
    & $iis "/config:$configFile" /site:CargoD01Demo /systray:false /trace:error
}
