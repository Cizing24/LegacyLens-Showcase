param([int]$Port=5194)
$ErrorActionPreference='Stop'
if ($Port -lt 1024 -or $Port -gt 65535 -or $Port -eq 5188) { throw 'Use a separate local E2E port.' }
$repo=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$frameworkShell=Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
foreach ($script in @('setup','seed','migrate')) {
    & $frameworkShell -NoProfile -File (Join-Path $repo "tools\database\$script.ps1") -DatabaseName LegacyLensShowcaseE2E
    if ($LASTEXITCODE -ne 0) { throw "Isolated E2E database $script failed." }
}
$target=Join-Path $repo ('.runtime\e2e-web-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $target -Force | Out-Null
Get-ChildItem (Join-Path $repo 'src\LegacyLens.Web') -File | Where-Object Extension -In '.aspx','.master','.config','.asax' | Copy-Item -Destination $target
Copy-Item -LiteralPath (Join-Path $repo 'src\LegacyLens.Web\Content'),(Join-Path $repo 'src\LegacyLens.Web\bin') -Destination $target -Recurse
$configPath=Join-Path $target 'Web.config'
[xml]$config=[IO.File]::ReadAllText($configPath)
$config.configuration.connectionStrings.add.connectionString='Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LegacyLensShowcaseE2E;Integrated Security=True;Connect Timeout=30'
$config.Save($configPath)
$iis=Join-Path $env:ProgramFiles 'IIS Express\iisexpress.exe'
$process=Start-Process -FilePath $iis -ArgumentList "/path:`"$target`" /port:$Port /clr:v4.0 /systray:false" -WindowStyle Hidden -PassThru -RedirectStandardOutput "$target\iis.log" -RedirectStandardError "$target\iis.error.log"
@{pid=$process.Id;port=$Port;database='LegacyLensShowcaseE2E';target=$target;url="http://localhost:$Port"} | ConvertTo-Json | Set-Content (Join-Path $repo '.runtime\e2e-server.json') -Encoding UTF8
Write-Output "Isolated IIS Express process $($process.Id): http://localhost:$Port / LegacyLensShowcaseE2E"
