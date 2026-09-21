[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration='Debug',[switch]$SkipTests)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if (-not (Get-Command msbuild.exe -ErrorAction SilentlyContinue)) { throw 'Open Developer PowerShell for VS 2022 first (ASP.NET workload and .NET Framework 4.8 targeting pack required).' }
& (Join-Path $PSScriptRoot 'Prepare-Prototype.ps1')
Push-Location $repo
try {
 & msbuild.exe 'CarTransport.sln' -restore /p:RestorePackagesConfig=true "/p:Configuration=$Configuration" /p:MvcBuildViews=true /m
 if($LASTEXITCODE -ne 0) { throw 'Build or Razor compilation failed. See MSBuild output.' }
 if(-not $SkipTests) {
  $test=Join-Path $repo "tests/VD.Tests/bin/$Configuration/net48/VD.Tests.dll"
  & vstest.console.exe $test /Logger:trx
  if($LASTEXITCODE -ne 0) { throw 'Tests failed. See TestResults.' }
 }
} finally { Pop-Location }
