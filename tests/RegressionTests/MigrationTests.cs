using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using LegacyLens.Domain;
using LegacyLens.Infrastructure;
using NUnit.Framework;

namespace LegacyLens.Tests
{
    [TestFixture,Category("Integration"),NonParallelizable]
    public class MigrationTests
    {
        private static string Root { get { return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,"..","..","..","..")); } }
        private static void Execute(Database db,string sql)
        {
            using(var connection=db.Open())
            using(var command=new SqlCommand(sql,connection)) { command.CommandTimeout=120;command.ExecuteNonQuery(); }
        }
        [Test]
        public void MigrationFailureRerunBackfillAndSafeRollbackPreserveLegacyData()
        {
            var name="LegacyLensMigrationTests_"+Guid.NewGuid().ToString("N");
            var master=new Database("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True");
            Execute(master,"CREATE DATABASE ["+name+"]");
            var db=new Database("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog="+name+";Integrated Security=True;Pooling=False");
            try
            {
                Execute(db,File.ReadAllText(Path.Combine(Root,"database/schema/001-baseline.sql")));
                Execute(db,@"INSERT dbo.Users(Username,PasswordHash,Role) VALUES('migration-user','unused-test-hash','Manager');
INSERT dbo.Customers(Name,Industry) VALUES(N'合成舊客戶',N'測試');
INSERT dbo.Cases(CustomerId,Title,Status,AssignedTo) VALUES(1,N'舊版案件保留','Open',1);
INSERT dbo.CaseHistory(CaseId,ActorId,Action,NewStatus,NewAssignee,CorrelationId) VALUES(1,1,'Created','Open',1,'migration-fixture');");
                var migration=File.ReadAllText(Path.Combine(Root,"database/migrations/003-reassignment.sql"));
                var fault=migration.Replace("INSERT dbo.SchemaVersions(Version) VALUES(3);","THROW 51009,'Controlled migration failure',1;");
                Assert.That(fault,Is.Not.EqualTo(migration));
                Assert.Throws<SqlException>(()=>Execute(db,fault));
                Assert.That(db.Query("SELECT COL_LENGTH('dbo.Cases','AssignmentPolicyVersion') AS Value").Rows[0]["Value"],Is.EqualTo(DBNull.Value));
                Assert.That(db.Query("SELECT Version FROM dbo.SchemaVersions WHERE Version=3").Rows.Count,Is.Zero);
                Execute(db,migration);Execute(db,migration);
                Assert.That((byte)db.Query("SELECT AssignmentPolicyVersion FROM dbo.Cases WHERE Id=1").Rows[0][0],Is.EqualTo(1));
                Assert.That(new CaseStore(db).Get(1).Title,Is.EqualTo("舊版案件保留"));
                Assert.That(new CaseStore(db).History(1).Rows.Count,Is.EqualTo(1));
                Assert.That(new CaseStore(db).History(1).Rows[0]["Reason"],Is.EqualTo(DBNull.Value));
                Execute(db,"INSERT dbo.Cases(CustomerId,Title,Status,AssignedTo) VALUES(1,N'新增案件','Open',1)");
                Assert.That((byte)db.Query("SELECT AssignmentPolicyVersion FROM dbo.Cases WHERE Id=2").Rows[0][0],Is.EqualTo(2));
                Execute(db,File.ReadAllText(Path.Combine(Root,"database/migrations/003-reassignment-rollback.sql")));
                Assert.That(new TransferStore(db).Enabled(),Is.False);
                Execute(db,migration);
                Assert.That(new TransferStore(db).Enabled(),Is.False,"Rerunning migration must not silently undo an operator rollback.");
                var request=new TransferRequest {RequestId=Guid.NewGuid(),CaseId=1,ToAssignee=2,Reason="回復期間不應轉派",Version=new CaseStore(db).Get(1).Version};
                Assert.Throws<ArgumentException>(()=>new TransferStore(db).Transfer(request,new Actor {Id=1,Role="Manager"},Guid.NewGuid().ToString()));
                Assert.That(db.Query("SELECT Id FROM dbo.Cases").Rows.Count,Is.EqualTo(2));
                Assert.That(new CaseStore(db).History(1).Rows.Count,Is.EqualTo(1));
                TestContext.WriteLine("Controlled migration failure fully rolled back; rerun/backfill/default/legacy history/feature rollback passed.");
            }
            finally
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(name,"^LegacyLensMigrationTests_[0-9a-f]{32}$")) throw new InvalidOperationException("Refusing cleanup of a non-owned database.");
                Execute(master,"ALTER DATABASE ["+name+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["+name+"]");
            }
        }
    }
}
