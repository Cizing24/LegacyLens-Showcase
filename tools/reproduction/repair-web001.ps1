$ErrorActionPreference='Stop'
$repo=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$state=Get-Content (Join-Path $repo '.runtime\web001.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$target=[IO.Path]::GetFullPath($state.target)
$allowed=[IO.Path]::GetFullPath((Join-Path $repo '.runtime'))+[IO.Path]::DirectorySeparatorChar
if (-not $target.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)) { throw 'Reproduction target is outside the allowed workspace.' }
[IO.File]::WriteAllText((Join-Path $target 'src\LegacyLens.Web\CaseEdit.aspx.cs'),[IO.File]::ReadAllText((Join-Path $repo 'src\LegacyLens.Web\CaseEdit.aspx.cs')))
$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild=& $vswhere -latest -products '*' -all -find 'MSBuild\Current\Bin\MSBuild.exe' | Select-Object -First 1
& $msbuild (Join-Path $target 'src\LegacyLens.Web\LegacyLens.Web.csproj') /p:Configuration=Release /v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Repair build failed.' }
Write-Output 'Restored guarded initial binding in the isolated application; rerun the same browser assertion.'
