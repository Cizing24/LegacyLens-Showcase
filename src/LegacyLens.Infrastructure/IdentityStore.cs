using System;
using System.Data;
using System.Data.SqlClient;
using LegacyLens.Domain;

namespace LegacyLens.Infrastructure
{
    public sealed class IdentityStore
    {
        private readonly Database db;
        private static readonly string dummyHash = Passwords.Hash("UnusedDummyPassword!");
        public IdentityStore(Database db) { this.db = db; }
        public Actor Find(string username)
        {
            var rows = db.Query("SELECT Id, Username, Role FROM dbo.Users WHERE Username=@name AND IsActive=1",
                Database.P("@name", SqlDbType.NVarChar, username, 80));
            return rows.Rows.Count == 0 ? null : Read(rows.Rows[0]);
        }
        public Actor Authenticate(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length > 80) return null;
            using (var connection = db.Open())
            using (var transaction = connection.BeginTransaction())
            using (var command = new SqlCommand("SELECT Id,Username,Role,PasswordHash,FailedAttempts,LockedUntil FROM dbo.Users WITH (UPDLOCK,ROWLOCK) WHERE Username=@name AND IsActive=1", connection, transaction))
            {
                command.Parameters.Add(Database.P("@name", SqlDbType.NVarChar, username, 80));
                var table = new DataTable();
                using (var reader = command.ExecuteReader()) table.Load(reader);
                if (table.Rows.Count == 0)
                {
                    Passwords.Verify(password, dummyHash);
                    transaction.Commit();
                    return null;
                }
                var row = table.Rows[0];
                bool locked = row["LockedUntil"] != DBNull.Value && (DateTime)row["LockedUntil"] > DateTime.UtcNow;
                bool valid = Passwords.Verify(password, (string)row["PasswordHash"]) && !locked;
                using (var update = new SqlCommand(valid
                    ? "UPDATE dbo.Users SET FailedAttempts=0,LockedUntil=NULL WHERE Id=@id"
                    : "UPDATE dbo.Users SET FailedAttempts=CASE WHEN LockedUntil<=SYSUTCDATETIME() THEN 1 ELSE FailedAttempts+1 END,LockedUntil=CASE WHEN LockedUntil>SYSUTCDATETIME() THEN LockedUntil WHEN LockedUntil IS NULL AND FailedAttempts>=4 THEN DATEADD(minute,5,SYSUTCDATETIME()) ELSE NULL END WHERE Id=@id", connection, transaction))
                {
                    update.Parameters.Add(Database.P("@id", SqlDbType.Int, row["Id"]));
                    update.ExecuteNonQuery();
                }
                transaction.Commit();
                return valid ? Read(row) : null;
            }
        }
        private static Actor Read(DataRow row)
        {
            return new Actor { Id = (int)row["Id"], Name = (string)row["Username"], Role = (string)row["Role"] };
        }
        public DataTable Agents()
        {
            return db.Query("SELECT Id,Username FROM dbo.Users WHERE IsActive=1 AND Role IN ('Manager','Agent') ORDER BY Id");
        }
    }
}
