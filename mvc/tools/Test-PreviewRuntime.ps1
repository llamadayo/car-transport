param([Parameter(Mandatory=$true)][string]$PreviewPath)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$preview=(Resolve-Path -LiteralPath $PreviewPath).Path
if (!$preview.StartsWith((Join-Path $root 'artifacts')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Only workspace preview artifacts are allowed.' }
$output=Join-Path $root ('TestResults\PreviewRuntime-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
Get-ChildItem -LiteralPath (Join-Path $preview 'bin') -Filter '*.dll' | Copy-Item -Destination $output
$references=@('CARGO.Web.dll','CARGO.Service.dll','CARGO.DTO.dll','System.Web.Mvc.dll') | ForEach-Object { '/r:'+(Join-Path $output $_) }
$program=Join-Path $output 'PreviewRuntimeTests.exe'
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /utf8output /r:System.Web.dll /r:System.Core.dll /r:System.ComponentModel.DataAnnotations.dll "/out:$program" $references (Join-Path $root 'tests\PreviewRuntimeTests.cs')
if($LASTEXITCODE -ne 0){throw 'Runtime test compilation failed'}
& $program
if($LASTEXITCODE -ne 0){throw 'Deployed preview runtime tests failed'}
