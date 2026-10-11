$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output = Join-Path $root 'TestResults\Queries'
New-Item -ItemType Directory -Path $output -Force | Out-Null
# Compile against existing local preview DLLs without running licensing targets, reading app settings,
# starting a web server, restoring packages or connecting to Oracle.
$libraries = @(
    'packages\Microsoft.AspNet.Mvc.5.3.0\lib\net45\System.Web.Mvc.dll',
    'packages\Microsoft.AspNet.Razor.3.3.0\lib\net45\System.Web.Razor.dll',
    'packages\Microsoft.AspNet.WebPages.3.3.0\lib\net45\System.Web.WebPages.dll',
    'packages\Microsoft.AspNet.WebPages.3.3.0\lib\net45\System.Web.WebPages.Razor.dll',
    'packages\Microsoft.AspNet.WebPages.3.3.0\lib\net45\System.Web.Helpers.dll',
    'packages\Dapper.1.50.5\lib\net451\Dapper.dll',
    'lib\local\Kendo.Mvc.dll'
) | ForEach-Object { Join-Path $root $_ }
$references = @('/r:System.Core.dll','/r:System.Web.dll','/r:System.Data.dll','/r:System.ComponentModel.DataAnnotations.dll',
    '/r:System.Runtime.Serialization.dll','/r:System.Xml.dll','/r:Microsoft.CSharp.dll')
$references += $libraries | ForEach-Object { '/r:' + $_ }
$sources = @('DTO','Service','Web\Models','Web\ViewModels') | ForEach-Object {
    Get-ChildItem -LiteralPath (Join-Path $root $_) -Filter '*.cs' -Recurse |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object { $_.FullName }
}
$sources += @('Web\Controllers\CARGO_D02Controller.cs','Web\Controllers\CARGO_D04Controller.cs',
    'Web\Controllers\CARGO_D06Controller.cs','tests\QueryTests.cs') | ForEach-Object { Join-Path $root $_ }
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$program = Join-Path $output 'QueryTests.exe'
& $csc /nologo /utf8output /langversion:5 /define:LOCAL_DEMO /target:exe "/out:$program" $references $sources
if ($LASTEXITCODE -ne 0) { throw 'Query test compilation failed.' }
$libraries | ForEach-Object { Copy-Item -LiteralPath $_ -Destination $output -Force }
& $program $root $output
if ($LASTEXITCODE -ne 0) { throw 'Query tests failed.' }
$views = @('CARGO_D02Index.cs','CARGO_D04Index.cs','CARGO_D06Index.cs') | ForEach-Object { Join-Path $output $_ }
& $csc /nologo /utf8output /langversion:5 /target:library "/out:$output\QueryViews.dll" "/r:$program" $references $views
if ($LASTEXITCODE -ne 0) { throw 'Query Razor view compilation failed.' }
Write-Output 'PASS D02/D04/D06 Razor generated-code compilation'
$serviceSources = @('DTO','Service') | ForEach-Object {
    Get-ChildItem -LiteralPath (Join-Path $root $_) -Filter '*.cs' -Recurse |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object { $_.FullName }
}
$oracle = Join-Path $root 'packages\Oracle.ManagedDataAccess.12.1.2400\lib\net40\Oracle.ManagedDataAccess.dll'
& $csc /nologo /utf8output /langversion:5 /target:library "/out:$output\OracleServiceCompile.dll" /r:System.Configuration.dll "/r:$oracle" $references $serviceSources
if ($LASTEXITCODE -ne 0) { throw 'Non-demo Service compilation failed.' }
Write-Output 'PASS non-demo Oracle Service compilation (no database connection)'
