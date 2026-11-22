param([string]$DatabaseName = 'LegacyLens')
$ErrorActionPreference = 'Stop'
if ($DatabaseName -notmatch '^LegacyLens[A-Za-z0-9_]*$') { throw 'Invalid database name.' }
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -Path (Join-Path $repo 'src\LegacyLens.Web\bin\LegacyLens.Infrastructure.dll')
Add-Type -AssemblyName System.Data
$connection = New-Object System.Data.SqlClient.SqlConnection "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=$DatabaseName;Integrated Security=True"
$connection.Open()
$transaction = $connection.BeginTransaction()
try {
    $check = $connection.CreateCommand(); $check.Transaction = $transaction
    $check.CommandText = 'SELECT COUNT(*) FROM dbo.Users'
    if ([int]$check.ExecuteScalar() -gt 0) { Write-Output 'Seed already exists; preserved existing users and credentials.'; $transaction.Commit(); return }
    $bytes = New-Object byte[] 24
    $random = [Security.Cryptography.RandomNumberGenerator]::Create(); $random.GetBytes($bytes); $random.Dispose()
    $password = [Convert]::ToBase64String($bytes) + '!'
    foreach ($entry in @(@('manager','Manager'),@('agent','Agent'),@('viewer','Viewer'),@('agent2','Agent'))) {
        $command = $connection.CreateCommand();$command.Transaction = $transaction
        $command.CommandText = 'INSERT dbo.Users(Username,PasswordHash,Role) VALUES(@name,@hash,@role)'
        $command.Parameters.Add('@name',[Data.SqlDbType]::NVarChar,80).Value = $entry[0]
        $command.Parameters.Add('@hash',[Data.SqlDbType]::VarChar,200).Value = [LegacyLens.Infrastructure.Passwords]::Hash($password)
        $command.Parameters.Add('@role',[Data.SqlDbType]::VarChar,16).Value = $entry[1]
        $command.ExecuteNonQuery() | Out-Null
    }
    $command = $connection.CreateCommand();$command.Transaction = $transaction
    $command.CommandText = @"
INSERT dbo.Customers(Name,Industry) VALUES(N'示範客戶：北區服務中心',N'合成服務單位'),(N'示範客戶：南區服務中心',N'合成服務單位'),(N'NYC 311 公開資料映射客戶',N'公開資料測試');
DECLARE @i int=1, @customer int=(SELECT MIN(Id) FROM dbo.Customers), @agent int=(SELECT Id FROM dbo.Users WHERE Username='agent');
WHILE @i<=45 BEGIN
INSERT dbo.Cases(CustomerId,Title,Description,Status,AssignedTo,CreatedAt) VALUES(@customer,N'合成案件：系統服務檢查 '+CONVERT(nvarchar(10),@i),N'固定合成資料，供 Baseline 與回歸測試。',CASE WHEN @i%3=0 THEN 'Closed' WHEN @i%3=1 THEN 'Open' ELSE 'InProgress' END,@agent,DATEADD(minute,-@i,SYSUTCDATETIME()));
SET @i+=1; END;
"@
    $command.ExecuteNonQuery() | Out-Null
    $runtime = Join-Path $repo '.runtime';New-Item -ItemType Directory -Path $runtime -Force | Out-Null
    @{ username='manager'; password=$password; database=$DatabaseName } | ConvertTo-Json | Set-Content (Join-Path $runtime "credentials-$DatabaseName.json") -Encoding UTF8
    $transaction.Commit()
    Write-Output "Seeded four synthetic users, three customers and 45 cases. Credentials are ignored under .runtime."
} catch { $transaction.Rollback(); throw } finally { $connection.Dispose() }
