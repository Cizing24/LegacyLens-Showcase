param([ValidateSet(10000,50000,100000)][int]$Limit = 100000, [string]$DatabaseName = 'LegacyLens')
$ErrorActionPreference = 'Stop'
if ($DatabaseName -notmatch '^LegacyLens[A-Za-z0-9_]*$') { throw 'Invalid isolated database name.' }
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -AssemblyName System.Data
$manifest = Get-Content (Join-Path $repo 'docs\datasets\snapshot-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$csv = Join-Path $repo '.runtime\data\nyc-311.csv'
if ((Get-FileHash $csv -Algorithm SHA256).Hash.ToLowerInvariant() -ne $manifest.csvSha256) { throw 'CSV checksum mismatch.' }
$records = @(Import-Csv -LiteralPath $csv -Encoding UTF8 | Select-Object -First $Limit)
if ($records.Count -ne $Limit) { throw 'Not enough source records for requested scale.' }
$table = New-Object Data.DataTable
foreach ($column in @('SourceKey','Title','SourceStatus','Agency','Status')) { $table.Columns.Add($column,[string]) | Out-Null }
$table.Columns.Add('CreatedAt',[datetime]) | Out-Null
$zone = [TimeZoneInfo]::FindSystemTimeZoneById('Eastern Standard Time')
foreach ($record in $records) {
    if ($record.unique_key -notmatch '^\d+$' -or $record.complaint_type.Length -gt 160 -or $record.status.Length -gt 60 -or $record.agency.Length -gt 40) { throw 'Source row violates mapping contract; do not truncate silently.' }
    $date = [datetime]::SpecifyKind([datetime]::Parse($record.created_date,[Globalization.CultureInfo]::InvariantCulture),[DateTimeKind]::Unspecified)
    $utc = [TimeZoneInfo]::ConvertTimeToUtc($date,$zone)
    $status = if ($record.status -eq 'Closed') { 'Closed' } else { 'Open' }
    $table.Rows.Add($record.unique_key,$record.complaint_type,$record.status,$record.agency,$status,$utc) | Out-Null
}
$connection = New-Object Data.SqlClient.SqlConnection "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=$DatabaseName;Integrated Security=True"
$connection.Open()
$transaction = $connection.BeginTransaction()
$watch = [Diagnostics.Stopwatch]::StartNew()
try {
    $command = $connection.CreateCommand();$command.Transaction = $transaction
    $command.CommandText = 'CREATE TABLE #Import(SourceKey varchar(32),Title nvarchar(160),SourceStatus nvarchar(60),Agency nvarchar(40),Status varchar(16),CreatedAt datetime2);'
    $command.ExecuteNonQuery() | Out-Null
    $bulk = New-Object Data.SqlClient.SqlBulkCopy($connection,[Data.SqlClient.SqlBulkCopyOptions]::Default,$transaction)
    $bulk.DestinationTableName = '#Import';$bulk.BulkCopyTimeout = 120
    foreach ($column in $table.Columns) { $bulk.ColumnMappings.Add($column.ColumnName,$column.ColumnName) | Out-Null }
    $bulk.WriteToServer($table);$bulk.Dispose()
    $command.CommandTimeout = 120
    $command.CommandText = @'
DECLARE @customer int=(SELECT Id FROM dbo.Customers WHERE Name=N'NYC 311 公開資料映射客戶');
DECLARE @agent int=(SELECT Id FROM dbo.Users WHERE Username='agent');
IF @customer IS NULL OR @agent IS NULL THROW 51000,'Run synthetic seed before import',1;
INSERT dbo.Cases(CustomerId,Title,Description,Status,AssignedTo,CreatedAt,SourceKey,SourceStatus,Agency)
SELECT @customer,s.Title,N'NYC 311 categorical record; customer and assignee are synthetic.',s.Status,@agent,s.CreatedAt,s.SourceKey,s.SourceStatus,s.Agency
FROM #Import s WHERE NOT EXISTS(SELECT 1 FROM dbo.Cases c WITH(UPDLOCK,HOLDLOCK) WHERE c.SourceKey=s.SourceKey);
SELECT @@ROWCOUNT;
'@
    $inserted = [int]$command.ExecuteScalar()
    $transaction.Commit();$watch.Stop()
    $result = @{ database=$DatabaseName; requested=$Limit; sourceRows=$records.Count; inserted=$inserted; elapsedMs=$watch.Elapsed.TotalMilliseconds; csvSha256=$manifest.csvSha256; utc=(Get-Date).ToUniversalTime().ToString('o') }
    $report = Join-Path $repo "docs\datasets\import-$DatabaseName-$Limit.json"
    $result | ConvertTo-Json | Set-Content -LiteralPath $report -Encoding UTF8
    $result | ConvertTo-Json -Compress
} catch { $transaction.Rollback();throw } finally { $connection.Dispose() }
