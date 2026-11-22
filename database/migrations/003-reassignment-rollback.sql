-- Safe feature rollback: retain additive schema, backfilled versions and every history row.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.FeatureFlags') IS NULL THROW 51000,'Migration 003 is not installed',1;
UPDATE dbo.FeatureFlags SET Enabled=0 WHERE Name='Reassignment';
COMMIT;
