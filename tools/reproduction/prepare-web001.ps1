param([int]$Port=5189)
$ErrorActionPreference='Stop'
if ($Port -eq 5188 -or $Port -lt 1024 -or $Port -gt 65535) { throw 'Use a separate local reproduction port.' }
$repo=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$target=Join-Path $repo ('.runtime\web001-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $target -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'src') -Destination $target -Recurse
$source=Join-Path $target 'src\LegacyLens.Web\CaseEdit.aspx.cs'
$code=[IO.File]::ReadAllText($source)
if (($code.Split(@('if (!IsPostBack)'),[StringSplitOptions]::None).Count-1) -ne 1) { throw 'Fault patch no longer matches; inspect lifecycle source.' }
[IO.File]::WriteAllText($source,$code.Replace('if (!IsPostBack)','if (true) // Controlled Fault Injection: rebinding overwrites posted edits.'))
$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild=& $vswhere -latest -products '*' -all -find 'MSBuild\Current\Bin\MSBuild.exe' | Select-Object -First 1
& $msbuild (Join-Path $target 'src\LegacyLens.Web\LegacyLens.Web.csproj') /p:Configuration=Release /v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Isolated fault build failed.' }
$iis=Join-Path $env:ProgramFiles 'IIS Express\iisexpress.exe'
$process=Start-Process -FilePath $iis -ArgumentList "/path:`"$target\src\LegacyLens.Web`" /port:$Port /clr:v4.0 /systray:false" -WindowStyle Hidden -PassThru -RedirectStandardOutput "$target\iis.log" -RedirectStandardError "$target\iis.error.log"
@{target=$target;pid=$process.Id;port=$Port;source=$source} | ConvertTo-Json | Set-Content (Join-Path $repo '.runtime\web001.json') -Encoding UTF8
Write-Output "Controlled fault running only in $target at localhost:$Port. Main source is unchanged."
