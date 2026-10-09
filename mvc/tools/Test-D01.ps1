param([string]$Csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe')
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = Join-Path $root 'TestResults'
New-Item -ItemType Directory -Path $output -Force | Out-Null
# Compile the actual DTOs, rule engine and mapper against the company MVC version.
# No Oracle server, Telerik runtime or test-framework package is needed here.
$sources = @(Get-ChildItem -LiteralPath (Join-Path $root 'DTO') -Filter '*.cs' -Recurse | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object { $_.FullName })
$sources += @((Join-Path $root 'Service\CARGO_D01Rules.cs'), (Join-Path $root 'Web\Models\CARGO_D01Mapper.cs'), (Join-Path $root 'Web\ViewModels\CARGO_D01\CARGO_D01ViewModels.cs'), (Join-Path $root 'tests\D01Tests.cs'))
$mvc = Join-Path $root 'packages\Microsoft.AspNet.Mvc.5.2.7\lib\net45\System.Web.Mvc.dll'
$program = Join-Path $output 'D01Tests.exe'
& $Csc /nologo /utf8output /langversion:5 /target:exe "/out:$program" /r:System.Core.dll /r:System.Web.dll /r:System.ComponentModel.DataAnnotations.dll "/r:$mvc" $sources
if ($LASTEXITCODE -ne 0) { throw 'D01 test compilation failed.' }
Copy-Item -LiteralPath $mvc -Destination $output -Force
& $program
if ($LASTEXITCODE -ne 0) { throw 'D01 tests failed.' }
