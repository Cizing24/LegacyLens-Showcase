param([int]$Port = 5188)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$iis = Join-Path $env:ProgramFiles 'IIS Express\iisexpress.exe'
if (-not (Test-Path $iis)) { throw 'IIS Express is not installed.' }
if ($Port -lt 1024 -or $Port -gt 65535) { throw 'Use a development port from 1024 through 65535.' }
New-Item -ItemType Directory -Path (Join-Path $repo '.runtime') -Force | Out-Null
$process = Start-Process -FilePath $iis -ArgumentList "/path:`"$repo\src\LegacyLens.Web`" /port:$Port /clr:v4.0 /systray:false" -WindowStyle Hidden -PassThru -RedirectStandardOutput "$repo\.runtime\iis-$Port.log" -RedirectStandardError "$repo\.runtime\iis-$Port.error.log"
@{ pid=$process.Id; port=$Port; url="http://localhost:$Port" } | ConvertTo-Json | Set-Content "$repo\.runtime\server-$Port.json"
Write-Output "IIS Express process $($process.Id): http://localhost:$Port"
