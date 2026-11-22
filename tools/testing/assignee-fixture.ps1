param([Parameter(Mandatory=$true)][ValidateSet('Create','Disable','Demote','Inspect','Restore')][string]$Operation,[int]$UserId,[int]$CaseId)
$ErrorActionPreference='Stop'
$repo=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Add-Type -Path (Join-Path $repo 'src\LegacyLens.Web\bin\LegacyLens.Infrastructure.dll')
Add-Type -AssemblyName System.Data
$connection=New-Object Data.SqlClient.SqlConnection 'Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LegacyLensShowcaseE2E;Integrated Security=True'
$connection.Open()
try {
    $command=$connection.CreateCommand()
    $command.CommandText='SELECT DB_NAME()'
    if ($command.ExecuteScalar() -ne 'LegacyLensShowcaseE2E') { throw 'Refusing a non-E2E database.' }
    if ($Operation -eq 'Create') {
        $username='e2e-history-'+[Guid]::NewGuid().ToString('N')
        $command.CommandText="INSERT dbo.Users(Username,PasswordHash,Role) OUTPUT INSERTED.Id VALUES(@name,@hash,'Agent')"
        $command.Parameters.Add('@name',[Data.SqlDbType]::NVarChar,80).Value=$username
        $command.Parameters.Add('@hash',[Data.SqlDbType]::VarChar,200).Value=[LegacyLens.Infrastructure.Passwords]::Hash([Guid]::NewGuid().ToString('N')+[Guid]::NewGuid().ToString('N'))
        @{userId=[int]$command.ExecuteScalar();username=$username;database='LegacyLensShowcaseE2E'} | ConvertTo-Json -Compress
        return
    }
    $command.Parameters.Add('@user',[Data.SqlDbType]::Int).Value=$UserId
    $command.CommandText="SELECT COUNT(*) FROM dbo.Users WHERE Id=@user AND Username LIKE 'e2e-history-%'"
    if ([int]$command.ExecuteScalar() -ne 1) { throw 'Refusing to mutate a user not owned by the assignee fixture.' }
    if ($Operation -eq 'Inspect') {
        $command.Parameters.Add('@case',[Data.SqlDbType]::Int).Value=$CaseId
        $command.CommandText=@'
SELECT c.AssignedTo,u.Username,
(SELECT COUNT(*) FROM dbo.CaseHistory WHERE CaseId=c.Id) AS HistoryCount,
(SELECT COUNT(*) FROM dbo.CaseHistory WHERE CaseId=c.Id AND Action='Created' AND NewAssignee=@user) AS OriginalHistoryCount,
(SELECT COUNT(*) FROM dbo.CaseHistory WHERE CaseId=c.Id AND Action='Reassigned' AND OldAssignee=@user) AS TransferHistoryCount,
(SELECT COUNT(*) FROM dbo.TransferOperations WHERE CaseId=c.Id AND FromAssignee=@user) AS TransferCount,
(SELECT COUNT(*) FROM dbo.AuditLog WHERE EntityId=c.Id AND Action='Reassigned') AS TransferAuditCount
FROM dbo.Cases c JOIN dbo.Users u ON c.AssignedTo=u.Id WHERE c.Id=@case;
'@
        $reader=$command.ExecuteReader()
        try {
            if (-not $reader.Read()) { throw 'Fixture case not found.' }
            $result=@{database='LegacyLensShowcaseE2E'}
            for ($i=0;$i -lt $reader.FieldCount;$i++) { $result[$reader.GetName($i)]=$reader.GetValue($i) }
            $result | ConvertTo-Json -Compress
        } finally { $reader.Dispose() }
        return
    }
    $command.CommandText=switch($Operation) {
        'Disable' { 'UPDATE dbo.Users SET IsActive=0 WHERE Id=@user' }
        'Demote' { "UPDATE dbo.Users SET Role='Viewer' WHERE Id=@user" }
        'Restore' { "UPDATE dbo.Users SET IsActive=1,Role='Agent' WHERE Id=@user" }
    }
    if ($command.ExecuteNonQuery() -ne 1) { throw 'Fixture mutation failed.' }
    @{database='LegacyLensShowcaseE2E';operation=$Operation;userId=$UserId} | ConvertTo-Json -Compress
} finally { $connection.Dispose() }
