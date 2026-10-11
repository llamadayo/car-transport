$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = Join-Path $root 'TestResults\D03D04Parity'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$sources = @('DTO','Service','Web\Models','Web\ViewModels') | ForEach-Object {
    Get-ChildItem -LiteralPath (Join-Path $root $_) -Filter '*.cs' -Recurse | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object { $_.FullName }
}
$sources += Join-Path $root 'tests\D03D04ParityTests.cs'
$mvc = Join-Path $root 'packages\Microsoft.AspNet.Mvc.5.2.7\lib\net45\System.Web.Mvc.dll'
$dapper = Join-Path $root 'packages\Dapper.1.50.5\lib\net451\Dapper.dll'
$program = Join-Path $output 'D03D04ParityTests.exe'
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /utf8output /langversion:5 /define:LOCAL_DEMO /target:exe "/out:$program" /r:System.Core.dll /r:System.Web.dll /r:System.ComponentModel.DataAnnotations.dll /r:System.Runtime.Serialization.dll /r:System.Xml.dll /r:System.Data.dll "/r:$mvc" "/r:$dapper" $sources
if ($LASTEXITCODE -ne 0) { throw 'D03/D04 parity test compilation failed.' }
Copy-Item -LiteralPath $mvc -Destination $output -Force
Copy-Item -LiteralPath $dapper -Destination $output -Force
& $program
if ($LASTEXITCODE -ne 0) { throw 'D03/D04 parity tests failed.' }
