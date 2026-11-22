SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Cases') AND name='IX_Cases_Status_Created')
CREATE INDEX IX_Cases_Status_Created ON dbo.Cases(Status,CreatedAt DESC,Id DESC) INCLUDE(Title,CustomerId,AssignedTo);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Cases') AND name='IX_Cases_Created')
CREATE INDEX IX_Cases_Created ON dbo.Cases(CreatedAt DESC,Id DESC) INCLUDE(Title,Status,CustomerId,AssignedTo);
IF NOT EXISTS(SELECT 1 FROM dbo.SchemaVersions WHERE Version=2) INSERT dbo.SchemaVersions(Version) VALUES(2);
COMMIT;
