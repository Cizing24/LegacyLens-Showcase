param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = & $vswhere -latest -products '*' -all -find 'MSBuild\Current\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw 'MSBuild not found. Install Visual Studio ASP.NET workload and .NET Framework 4.8 Developer Pack.' }
New-Item -ItemType Directory -Path (Join-Path $repo '.runtime') -Force | Out-Null
& $msbuild (Join-Path $repo 'LegacyLens.sln') /m /v:minimal /p:Configuration=$Configuration /bl:"$repo\.runtime\build.binlog"
if ($LASTEXITCODE -ne 0) { throw "MSBuild failed: $LASTEXITCODE" }
