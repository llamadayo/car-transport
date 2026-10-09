$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = Join-Path $root 'TestResults\Demo'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$sources = @(Get-ChildItem (Join-Path $root 'DTO') -Filter '*.cs' -Recurse | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object { $_.FullName })
$sources += @('Service\CARGO_D01Rules.cs','Service\CARGO_D01DemoData.cs','Service\CARGO_D01Service.Demo.cs','tests\D01DemoTests.cs') | ForEach-Object { Join-Path $root $_ }
$program = Join-Path $output 'D01DemoTests.exe'
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /utf8output /langversion:5 /define:LOCAL_DEMO /target:exe "/out:$program" /r:System.Core.dll /r:System.Runtime.Serialization.dll /r:System.Xml.dll $sources
if ($LASTEXITCODE -ne 0) { throw 'Demo test compilation failed.' }
& $program
if ($LASTEXITCODE -ne 0) { throw 'Demo tests failed.' }
