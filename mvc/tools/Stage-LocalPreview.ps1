param([switch]$Demo)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$source = Join-Path $root 'Web'
$buildFolder = if ($Demo) { 'LocalDemo' } else { 'LocalPreview' }
$stage = Join-Path $root ('artifacts\' + $buildFolder + '-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage -Force | Out-Null
foreach ($name in @('Views', 'Scripts', 'Content')) { Copy-Item -LiteralPath (Join-Path $source $name) -Destination $stage -Recurse }
Copy-Item -LiteralPath (Join-Path $source 'Global.asax') -Destination $stage
New-Item -ItemType Directory -Path (Join-Path $stage 'bin') -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $source ('bin\' + $buildFolder)) -File | Copy-Item -Destination (Join-Path $stage 'bin')
# Only rewrite freshly staged, non-secret configuration. Company source stays on MVC 5.2.7.
$config = [IO.File]::ReadAllText((Join-Path $source 'Web.config')).Replace('5.2.7.0','5.3.0.0').Replace('9.0.0.0','13.0.0.0')
$temp = Join-Path $stage 'aspnet-temp'
New-Item -ItemType Directory -Path $temp -Force | Out-Null
$config = $config.Replace('<compilation targetFramework="4.8" />', '<compilation targetFramework="4.8" tempDirectory="' + $temp + '" />')
$config = $config.Replace('</assemblyBinding>', '<dependentAssembly><assemblyIdentity name="Microsoft.Web.Infrastructure" publicKeyToken="31bf3856ad364e35" culture="neutral" /><bindingRedirect oldVersion="0.0.0.0-2.0.0.0" newVersion="2.0.0.0" /></dependentAssembly></assemblyBinding>')
if ($Demo) { $config = $config.Replace('<appSettings>', '<appSettings><add key="CARGO.LocalDemo" value="true" />') }
[IO.File]::WriteAllText((Join-Path $stage 'Web.config'), $config, [Text.UTF8Encoding]::new($false))
$viewConfig = Join-Path $stage 'Views\Web.config'
[IO.File]::WriteAllText($viewConfig, [IO.File]::ReadAllText($viewConfig).Replace('5.2.7.0','5.3.0.0'), [Text.UTF8Encoding]::new($false))
Write-Output $stage
