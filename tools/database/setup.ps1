param([string]$DatabaseName = 'LegacyLens')
$ErrorActionPreference = 'Stop'
if ($DatabaseName -notmatch '^LegacyLens[A-Za-z0-9_]*$') { throw 'Database must have a LegacyLens prefix and alphanumeric suffix.' }
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -AssemblyName System.Data
$localdb = Get-ChildItem 'C:\Program Files\Microsoft SQL Server' -Filter SqlLocalDB.exe -Recurse -ErrorAction SilentlyContinue | Sort-Object { $_.VersionInfo.FileMajorPart } -Descending | Select-Object -First 1 -ExpandProperty FullName
if (-not $localdb) { throw 'SQL Server LocalDB is missing.' }
$instances = @(& $localdb info)
if ($instances -notcontains 'MSSQLLocalDB') {
    $versions = @(& $localdb versions)
    $engine = $versions | ForEach-Object { if ($_ -match '\((\d+\.\d+)\.') { $Matches[1] } } | Sort-Object { [version]$_ } -Descending | Select-Object -First 1
    if (-not $engine) { throw 'No installed LocalDB engine version was found.' }
    & $localdb create MSSQLLocalDB $engine
    if ($LASTEXITCODE -ne 0) { throw 'Explicit LocalDB instance creation failed.' }
}
& $localdb start MSSQLLocalDB
if ($LASTEXITCODE -ne 0) { throw 'LocalDB failed to start.' }
$master = New-Object System.Data.SqlClient.SqlConnection 'Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Connect Timeout=60'
$master.Open()
try {
    $command = $master.CreateCommand()
    $command.CommandText = "IF DB_ID(N'$DatabaseName') IS NULL CREATE DATABASE [$DatabaseName]"
    $command.ExecuteNonQuery() | Out-Null
} finally { $master.Dispose() }
$connectionString = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=$DatabaseName;Integrated Security=True;Connect Timeout=60"
$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
$connection.Open()
try {
    $command = $connection.CreateCommand()
    $command.CommandText = [IO.File]::ReadAllText((Join-Path $repo 'database\schema\001-baseline.sql'))
    $command.ExecuteNonQuery() | Out-Null
    Write-Output "Schema applied to $DatabaseName."
} finally { $connection.Dispose() }
