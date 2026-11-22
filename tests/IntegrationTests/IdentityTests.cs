using System;
using System.Data;
using System.Data.SqlClient;
using LegacyLens.Infrastructure;
using NUnit.Framework;

namespace LegacyLens.Tests
{
    [TestFixture,Category("Integration"),NonParallelizable]
    public class IdentityTests : DatabaseFixture
    {
        [Test]
        public void LockoutExpiryAndSuccessfulLoginResetAreEnforced()
        {
            var username="auth-"+Guid.NewGuid().ToString("N");var password="SyntheticAuthTest!2026";
            using(var connection=db.Open())
            using(var command=new SqlCommand("INSERT dbo.Users(Username,PasswordHash,Role) VALUES(@name,@hash,'Viewer')",connection))
            {
                command.Parameters.Add(Database.P("@name",SqlDbType.NVarChar,username,80));command.Parameters.Add(Database.P("@hash",SqlDbType.VarChar,Passwords.Hash(password),200));command.ExecuteNonQuery();
            }
            var store=new IdentityStore(db);
            for(var i=0;i<5;i++) Assert.That(store.Authenticate(username,"wrong"),Is.Null);
            Assert.That(store.Authenticate(username,password),Is.Null,"Correct password must not bypass active lockout.");
            using(var connection=db.Open())
            using(var command=new SqlCommand("UPDATE dbo.Users SET LockedUntil=DATEADD(minute,-1,SYSUTCDATETIME()) WHERE Username=@name",connection))
            { command.Parameters.Add(Database.P("@name",SqlDbType.NVarChar,username,80));command.ExecuteNonQuery(); }
            Assert.That(store.Authenticate(username,"wrong"),Is.Null);
            var row=db.Query("SELECT FailedAttempts,LockedUntil FROM dbo.Users WHERE Username=@name",Database.P("@name",SqlDbType.NVarChar,username,80)).Rows[0];
            Assert.That(row["FailedAttempts"],Is.EqualTo(1));Assert.That(row["LockedUntil"],Is.EqualTo(DBNull.Value));
            Assert.That(store.Authenticate(username,password),Is.Not.Null);
            Assert.That(db.Query("SELECT FailedAttempts FROM dbo.Users WHERE Username=@name",Database.P("@name",SqlDbType.NVarChar,username,80)).Rows[0][0],Is.EqualTo(0));
        }
    }
}
