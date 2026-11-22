param([string]$DatabaseName='LegacyLens')
$ErrorActionPreference='Stop'
if ($DatabaseName -notmatch '^LegacyLens[A-Za-z0-9_]*$') { throw 'Invalid database name.' }
$repo=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -AssemblyName System.Data
$connection=New-Object Data.SqlClient.SqlConnection "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=$DatabaseName;Integrated Security=True"
$connection.Open()
try {
    foreach($migration in Get-ChildItem (Join-Path $repo 'database\migrations') -Filter '*.sql' | Where-Object Name -NotMatch 'rollback' | Sort-Object Name) {
        $command=$connection.CreateCommand();$command.CommandTimeout=120;$command.CommandText=[IO.File]::ReadAllText($migration.FullName)
        try { $command.ExecuteNonQuery() | Out-Null;Write-Output "Applied $($migration.Name) to $DatabaseName" } finally { $command.Dispose() }
    }
} finally { $connection.Dispose() }
