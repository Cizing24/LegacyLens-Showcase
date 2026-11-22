param([ValidateSet(10000,50000,100000)][int]$Scale = 100000,[int]$Iterations = 40,[string]$DatabaseName='LegacyLens')
$ErrorActionPreference = 'Stop'
if ($Iterations -lt 20 -or $Iterations -gt 1000) { throw 'Use 20-1000 measured iterations.' }
if ($DatabaseName -notmatch '^LegacyLens[A-Za-z0-9_]*$') { throw 'Use an isolated LegacyLens-prefixed database.' }
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -AssemblyName System.Data
$connection = New-Object Data.SqlClient.SqlConnection "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=$DatabaseName;Integrated Security=True"
$connection.Open()
$output = Join-Path $repo "docs\benchmarks\$Scale"
New-Item -ItemType Directory -Path $output -Force | Out-Null
function Execute-Sql([string]$sql) {
    $command=$connection.CreateCommand();$command.CommandText=$sql;$command.CommandTimeout=120
    try { $command.ExecuteNonQuery() | Out-Null } finally { $command.Dispose() }
}
function New-Query([string]$sql,[int]$offset=0) {
    $command=$connection.CreateCommand();$command.CommandText=$sql;$command.CommandTimeout=120
    $command.Parameters.Add('@title',[Data.SqlDbType]::NVarChar,160).Value=''
    $command.Parameters.Add('@status',[Data.SqlDbType]::VarChar,16).Value='Open'
    $command.Parameters.Add('@offset',[Data.SqlDbType]::Int).Value=$offset
    return $command
}
function Read-Results($command) {
    $reader=$command.ExecuteReader()
    $ids=New-Object 'Collections.Generic.List[int]'
    try { while($reader.Read()) { $ids.Add($reader.GetInt32(0)) } } finally { $reader.Dispose() }
    return ,$ids.ToArray()
}
try {
    $count=$connection.CreateCommand();$count.CommandText='SELECT COUNT(*) FROM dbo.Cases WHERE SourceKey IS NOT NULL'
    $sourceRows=[int]$count.ExecuteScalar()
    if ($sourceRows -ne $Scale) { throw "Expected exactly $Scale public rows; found $sourceRows. Use fresh scale or cumulative import in ascending scale order." }
    Execute-Sql "IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Cases') AND name='IX_Cases_Status_Created') DROP INDEX IX_Cases_Status_Created ON dbo.Cases; IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Cases') AND name='IX_Cases_Created') DROP INDEX IX_Cases_Created ON dbo.Cases; UPDATE STATISTICS dbo.Cases WITH FULLSCAN;"
    $baseline=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'baseline.sql'))
    $rewritten=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'rewritten.sql'))
    $reference=@{}
    foreach($offset in @(0,20,200)) { $command=New-Query $baseline $offset;try { $reference[$offset]=(Read-Results $command) -join ',' } finally { $command.Dispose() } }
    $results=@()
    foreach($stage in @('baseline','rewrite-only','indexed')) {
        if ($stage -eq 'indexed') { Execute-Sql ([IO.File]::ReadAllText((Join-Path $repo 'database\migrations\002-search-index.sql'))) }
        $sql=if($stage -eq 'baseline'){$baseline}else{$rewritten}
        $command=New-Query $sql
        for($warm=0;$warm -lt 5;$warm++) { $null=Read-Results $command }
        $samples=@()
        for($iteration=0;$iteration -lt $Iterations;$iteration++) {
            $watch=[Diagnostics.Stopwatch]::StartNew();$null=Read-Results $command;$watch.Stop()
            $samples += $watch.Elapsed.TotalMilliseconds
        }
        $command.Dispose()
        foreach($offset in @(0,20,200)) {
            $check=New-Query $sql $offset
            try { if (((Read-Results $check) -join ',') -ne $reference[$offset]) { throw "Result mismatch: $stage offset $offset" } } finally { $check.Dispose() }
        }
        $messages=New-Object 'Collections.Generic.List[string]'
        $handler=[Data.SqlClient.SqlInfoMessageEventHandler]{param($sender,$eventArgs) $messages.Add($eventArgs.Message)}
        $connection.add_InfoMessage($handler)
        Execute-Sql 'SET STATISTICS IO ON; SET STATISTICS XML ON;'
        $capture=New-Query $sql
        $reader=$capture.ExecuteReader()
        try {
            do {
                while($reader.Read()) {
                    if ($reader.FieldCount -eq 1 -and $reader.GetName(0) -like '*Showplan*') {
                        [IO.File]::WriteAllText((Join-Path $output "$stage.sqlplan"),[string]$reader.GetValue(0))
                    }
                }
            } while($reader.NextResult())
        } finally { $reader.Dispose();$capture.Dispose() }
        Execute-Sql 'SET STATISTICS IO OFF; SET STATISTICS XML OFF;'
        $connection.remove_InfoMessage($handler)
        [IO.File]::WriteAllLines((Join-Path $output "$stage-io.txt"),$messages)
        $sorted=@($samples | Sort-Object)
        $reads=@([regex]::Matches(($messages -join ' '),'logical reads (\d+)') | ForEach-Object { [int]$_.Groups[1].Value })
        if ($reads.Count -eq 0) { throw 'STATISTICS IO was not captured; do not report zero reads.' }
        $totalReads=($reads | Measure-Object -Sum).Sum
        $results += @{ stage=$stage;iterations=$Iterations;warmups=5;samplesMs=$samples;p50Ms=$sorted[[math]::Ceiling($Iterations*.5)-1];p95Ms=$sorted[[math]::Ceiling($Iterations*.95)-1];logicalReads=$totalReads;resultEquivalent=$true;checkedOffsets=@(0,20,200) }
    }
    $environment=$connection.CreateCommand();$environment.CommandText="SELECT @@VERSION AS Version,(SELECT COUNT(*) FROM dbo.Cases) AS TotalRows,(SELECT SUM(used_page_count)*8 FROM sys.dm_db_partition_stats WHERE object_id=OBJECT_ID('dbo.Cases') AND index_id>1) AS NonclusteredIndexKB"
    $reader=$environment.ExecuteReader();$reader.Read() | Out-Null
    $engine=$reader.GetString(0);$total=$reader.GetInt32(1);$indexKB=$reader.GetValue(2);$reader.Dispose()
    $hardware=Get-CimInstance Win32_ComputerSystem
    $report=@{utc=(Get-Date).ToUniversalTime().ToString('o');sourceRows=$sourceRows;totalRows=$total;sqlServer=$engine;memoryBytes=$hardware.TotalPhysicalMemory;logicalProcessors=$hardware.NumberOfLogicalProcessors;cache='warm; no DBCC cache clearing';connection='one open local integrated connection; sequential stages; single client';projection='Id,Title,Status,CreatedAt,Assignee,Customer; 21 rows';filter="Status='Open', title='', offset=0";nonclusteredIndexKB=$indexKB;results=$results}
    $report | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $output 'results.json') -Encoding UTF8
    $results | ForEach-Object { [pscustomobject]$_ } | Select-Object stage,p50Ms,p95Ms,logicalReads | ConvertTo-Json -Compress
} finally { $connection.Dispose() }
