SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.SchemaVersions') IS NULL
CREATE TABLE dbo.SchemaVersions(Version int NOT NULL PRIMARY KEY, AppliedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.SchemaVersions WHERE Version=1)
BEGIN
CREATE TABLE dbo.Users (
 Id int IDENTITY PRIMARY KEY, Username nvarchar(80) NOT NULL UNIQUE,
 PasswordHash varchar(200) NOT NULL, Role varchar(16) NOT NULL CHECK(Role IN ('Manager','Agent','Viewer')),
 IsActive bit NOT NULL DEFAULT 1, FailedAttempts int NOT NULL DEFAULT 0, LockedUntil datetime2 NULL
);
CREATE TABLE dbo.Customers (
 Id int IDENTITY PRIMARY KEY, Name nvarchar(120) NOT NULL, Industry nvarchar(80) NOT NULL,
 Version rowversion NOT NULL
);
CREATE TABLE dbo.Cases (
 Id int IDENTITY PRIMARY KEY, CustomerId int NOT NULL REFERENCES dbo.Customers(Id),
 Title nvarchar(160) NOT NULL, Description nvarchar(2000) NOT NULL DEFAULT N'',
 Status varchar(16) NOT NULL CHECK(Status IN ('Open','InProgress','Closed')),
 AssignedTo int NOT NULL REFERENCES dbo.Users(Id), CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
 UpdatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
 SourceKey varchar(32) NULL, SourceStatus nvarchar(60) NULL, Agency nvarchar(40) NULL,
 Version rowversion NOT NULL
);
CREATE UNIQUE INDEX UX_Cases_SourceKey ON dbo.Cases(SourceKey) WHERE SourceKey IS NOT NULL;
CREATE TABLE dbo.CaseHistory (
 Id bigint IDENTITY PRIMARY KEY, CaseId int NOT NULL REFERENCES dbo.Cases(Id),
 ActorId int NOT NULL REFERENCES dbo.Users(Id), Action varchar(40) NOT NULL,
 OldStatus varchar(16) NULL, NewStatus varchar(16) NOT NULL,
 OldAssignee int NULL, NewAssignee int NOT NULL,
 OccurredAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CorrelationId varchar(36) NOT NULL
);
CREATE INDEX IX_History_Case ON dbo.CaseHistory(CaseId,Id);
CREATE TABLE dbo.AuditLog (
 Id bigint IDENTITY PRIMARY KEY, ActorId int NOT NULL REFERENCES dbo.Users(Id),
 Action varchar(40) NOT NULL, EntityId int NOT NULL,
 OccurredAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CorrelationId varchar(36) NOT NULL
);
INSERT dbo.SchemaVersions(Version) VALUES(1);
END;
COMMIT;
