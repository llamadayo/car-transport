$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = Join-Path $root 'TestResults\D05D06Parity'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$sources = @('DTO','Service','Web\Models','Web\ViewModels') | ForEach-Object {
    Get-ChildItem -LiteralPath (Join-Path $root $_) -Filter '*.cs' -Recurse | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object { $_.FullName }
}
$sources += Join-Path $root 'tests\D05D06ParityTests.cs'
$sources += Join-Path $root 'Web\Controllers\CARGO_D05Controller.cs'
$kendo = Join-Path $root 'lib\local\Kendo.Mvc.dll'
$mvc = Join-Path $root 'packages\Microsoft.AspNet.Mvc.5.3.0\lib\net45\System.Web.Mvc.dll'
$dapper = Join-Path $root 'packages\Dapper.1.50.5\lib\net451\Dapper.dll'
$program = Join-Path $output 'D05D06ParityTests.exe'
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /utf8output /langversion:5 /define:LOCAL_DEMO /target:exe "/out:$program" /r:System.Core.dll /r:System.Web.dll /r:System.ComponentModel.DataAnnotations.dll /r:System.Runtime.Serialization.dll /r:System.Xml.dll /r:System.Data.dll "/r:$mvc" "/r:$kendo" "/r:$dapper" $sources
if ($LASTEXITCODE -ne 0) { throw 'D05/D06 parity test compilation failed.' }
Copy-Item -LiteralPath $kendo -Destination $output -Force
Copy-Item -LiteralPath $mvc -Destination $output -Force
Copy-Item -LiteralPath $dapper -Destination $output -Force
@('Microsoft.AspNet.Razor.3.3.0\lib\net45\System.Web.Razor.dll', 'Microsoft.AspNet.WebPages.3.3.0\lib\net45\System.Web.WebPages.dll', 'Microsoft.AspNet.WebPages.3.3.0\lib\net45\System.Web.WebPages.Razor.dll', 'Microsoft.AspNet.WebPages.3.3.0\lib\net45\System.Web.Helpers.dll') | ForEach-Object { Copy-Item -LiteralPath (Join-Path $root ('packages\' + $_)) -Destination $output -Force }
& $program
if ($LASTEXITCODE -ne 0) { throw 'D05/D06 parity tests failed.' }
