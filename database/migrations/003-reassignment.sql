SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS(SELECT 1 FROM dbo.SchemaVersions WITH(UPDLOCK,HOLDLOCK) WHERE Version=3)
BEGIN
ALTER TABLE dbo.Cases ADD AssignmentPolicyVersion tinyint NOT NULL CONSTRAINT DF_Cases_AssignmentPolicy DEFAULT(1) WITH VALUES;
ALTER TABLE dbo.Cases DROP CONSTRAINT DF_Cases_AssignmentPolicy;
ALTER TABLE dbo.Cases ADD CONSTRAINT DF_Cases_AssignmentPolicy DEFAULT(2) FOR AssignmentPolicyVersion;
ALTER TABLE dbo.CaseHistory ADD Reason nvarchar(500) NULL, RequestId uniqueidentifier NULL;
CREATE TABLE dbo.TransferOperations (
 RequestId uniqueidentifier NOT NULL PRIMARY KEY,
 CaseId int NOT NULL REFERENCES dbo.Cases(Id), FromAssignee int NOT NULL REFERENCES dbo.Users(Id),
 ToAssignee int NOT NULL REFERENCES dbo.Users(Id), ActorId int NOT NULL REFERENCES dbo.Users(Id),
 Reason nvarchar(500) NOT NULL, OccurredAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
EXEC(N'CREATE UNIQUE INDEX UX_History_RequestId ON dbo.CaseHistory(RequestId) WHERE RequestId IS NOT NULL');
CREATE TABLE dbo.FeatureFlags(Name varchar(40) PRIMARY KEY,Enabled bit NOT NULL);
INSERT dbo.FeatureFlags(Name,Enabled) VALUES('Reassignment',1);
INSERT dbo.SchemaVersions(Version) VALUES(3);
END;
COMMIT;
