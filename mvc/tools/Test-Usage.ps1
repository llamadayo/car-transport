$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = Join-Path $root 'TestResults\Usage'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$sources = @(Get-ChildItem (Join-Path $root 'DTO') -Filter '*.cs' -Recurse | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object { $_.FullName })
$sources += @(Get-ChildItem (Join-Path $root 'Service') -Filter '*.cs' | Where-Object { $_.Name -ne 'CARGO_DService.cs' } | ForEach-Object { $_.FullName })
$sources += Join-Path $root 'tests\UsageTests.cs'
$program = Join-Path $output 'UsageTests.exe'
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /utf8output /langversion:5 /define:LOCAL_DEMO /target:exe "/out:$program" /r:System.Core.dll /r:System.Runtime.Serialization.dll /r:System.Xml.dll $sources
if ($LASTEXITCODE -ne 0) { throw 'Usage test compilation failed.' }
& $program
if ($LASTEXITCODE -ne 0) { throw 'Usage tests failed.' }
