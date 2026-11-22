using System;
using System.Data;
using System.Data.SqlClient;
using LegacyLens.Application;
using LegacyLens.Domain;

namespace LegacyLens.Infrastructure
{
    public sealed class CaseStore : ICaseStore
    {
        private readonly Database db;
        public CaseStore(Database db) { this.db = db; }
        public CaseRecord Get(int id)
        {
            var rows = db.Query("SELECT * FROM dbo.Cases WHERE Id=@id", Database.P("@id", SqlDbType.Int, id));
            if (rows.Rows.Count == 0) throw new ArgumentException("找不到案件。");
            return Read(rows.Rows[0]);
        }
        public static CaseRecord Read(DataRow row)
        {
            return new CaseRecord {
                Id = (int)row["Id"], CustomerId = (int)row["CustomerId"], Title = (string)row["Title"],
                Description = (string)row["Description"], Status = (string)row["Status"],
                AssignedTo = (int)row["AssignedTo"], CreatedAt = (DateTime)row["CreatedAt"], Version = (byte[])row["Version"]
            };
        }
        public int Create(CaseRecord item, Actor actor, string correlationId)
        {
            using (var connection = db.Open())
            using (var tx = connection.BeginTransaction())
            {
                CaseRules.Validate(item, actor, null);
                CheckAssignee(connection, tx, item.AssignedTo);
                using (var command = new SqlCommand(@"INSERT dbo.Cases(CustomerId,Title,Description,Status,AssignedTo)
OUTPUT INSERTED.Id VALUES(@customer,@title,@description,@status,@assignee)", connection, tx))
                {
                    AddValues(command, item);
                    item.Id = (int)command.ExecuteScalar();
                }
                History(connection, tx, item, null, actor, correlationId, "Created");
                tx.Commit();
                return item.Id;
            }
        }
        public void Update(CaseRecord item, Actor actor, string correlationId)
        {
            using (var connection = db.Open())
            using (var tx = connection.BeginTransaction())
            {
                CaseRecord old;
                using (var read = new SqlCommand("SELECT * FROM dbo.Cases WITH(UPDLOCK,HOLDLOCK) WHERE Id=@id", connection, tx))
                {
                    read.Parameters.Add(Database.P("@id", SqlDbType.Int, item.Id));
                    var rows = new DataTable();
                    using (var reader = read.ExecuteReader()) rows.Load(reader);
                    if (rows.Rows.Count == 0) throw new ArgumentException("找不到案件。");
                    old = Read(rows.Rows[0]);
                }
                CaseRules.Validate(item, actor, old);
                CheckAssignee(connection, tx, item.AssignedTo);
                using (var command = new SqlCommand(@"UPDATE dbo.Cases SET CustomerId=@customer,Title=@title,
Description=@description,Status=@status,AssignedTo=@assignee,UpdatedAt=SYSUTCDATETIME()
WHERE Id=@id AND Version=@version", connection, tx))
                {
                    AddValues(command, item);
                    command.Parameters.Add(Database.P("@id", SqlDbType.Int, item.Id));
                    command.Parameters.Add(Database.P("@version", SqlDbType.Binary, item.Version, 8));
                    if (command.ExecuteNonQuery() != 1) throw new ConflictException();
                }
                History(connection, tx, item, old, actor, correlationId, "Updated");
                tx.Commit();
            }
        }
        private static void CheckAssignee(SqlConnection connection, SqlTransaction tx, int id)
        {
            using (var command = new SqlCommand("SELECT COUNT(*) FROM dbo.Users WITH(HOLDLOCK) WHERE Id=@id AND IsActive=1 AND Role IN ('Agent','Manager')", connection, tx))
            {
                command.Parameters.Add(Database.P("@id", SqlDbType.Int, id));
                if ((int)command.ExecuteScalar() != 1) throw new ArgumentException("承辦人已停用或無處理權限。");
            }
        }
        private static void AddValues(SqlCommand command, CaseRecord item)
        {
            command.Parameters.AddRange(new[] {
                Database.P("@customer",SqlDbType.Int,item.CustomerId), Database.P("@title",SqlDbType.NVarChar,item.Title,160),
                Database.P("@description",SqlDbType.NVarChar,item.Description ?? "",2000), Database.P("@status",SqlDbType.VarChar,item.Status,16),
                Database.P("@assignee",SqlDbType.Int,item.AssignedTo)
            });
        }
        private static void History(SqlConnection connection, SqlTransaction tx, CaseRecord item, CaseRecord old, Actor actor, string correlationId, string action)
        {
            using (var command = new SqlCommand(@"INSERT dbo.CaseHistory(CaseId,ActorId,Action,OldStatus,NewStatus,OldAssignee,NewAssignee,CorrelationId)
VALUES(@id,@actor,@action,@oldStatus,@status,@oldAssignee,@assignee,@correlation);
INSERT dbo.AuditLog(ActorId,Action,EntityId,CorrelationId) VALUES(@actor,@action,@id,@correlation)", connection, tx))
            {
                command.Parameters.AddRange(new[] {
                    Database.P("@id",SqlDbType.Int,item.Id), Database.P("@actor",SqlDbType.Int,actor.Id),
                    Database.P("@action",SqlDbType.VarChar,action,40), Database.P("@oldStatus",SqlDbType.VarChar,old == null ? null : old.Status,16),
                    Database.P("@status",SqlDbType.VarChar,item.Status,16), Database.P("@oldAssignee",SqlDbType.Int,old == null ? (object)null : old.AssignedTo),
                    Database.P("@assignee",SqlDbType.Int,item.AssignedTo), Database.P("@correlation",SqlDbType.VarChar,correlationId,36)
                });
                command.ExecuteNonQuery();
            }
        }
        public DataTable Search(string title, string status, int page)
        {
            if (page < 0 || page > 50000) throw new ArgumentException("頁碼無效。");
            if ((title ?? "").Length > 160) throw new ArgumentException("搜尋字串過長。");
            if (!string.IsNullOrEmpty(status) && status != "Open" && status != "Closed" && status != "InProgress")
                throw new ArgumentException("搜尋狀態無效。");
            var predicate = "WHERE 1=1";
            if (!string.IsNullOrEmpty(title)) predicate += " AND c.Title LIKE N'%'+@title+N'%'";
            if (!string.IsNullOrEmpty(status)) predicate += " AND c.Status=@status";
            return db.Query(@"SELECT c.Id,c.Title,c.Status,c.CreatedAt,u.Username AS Assignee,k.Name AS Customer
FROM dbo.Cases c JOIN dbo.Users u ON c.AssignedTo=u.Id JOIN dbo.Customers k ON c.CustomerId=k.Id
" + predicate + @"
ORDER BY c.CreatedAt DESC,c.Id DESC OFFSET @offset ROWS FETCH NEXT 21 ROWS ONLY",
                Database.P("@title", SqlDbType.NVarChar, title ?? "", 160), Database.P("@status", SqlDbType.VarChar, status ?? "", 16),
                Database.P("@offset", SqlDbType.Int, page * 20));
        }
        public DataTable History(int id)
        {
            return db.Query(@"SELECT h.Action,h.OldStatus,h.NewStatus,u.Username AS Actor,a.Username AS Assignee,h.OccurredAt,h.CorrelationId,h.Reason
FROM dbo.CaseHistory h JOIN dbo.Users u ON h.ActorId=u.Id JOIN dbo.Users a ON h.NewAssignee=a.Id
WHERE CaseId=@id ORDER BY h.Id DESC", Database.P("@id", SqlDbType.Int, id));
        }
        public DataTable Statistics()
        {
            return db.Query("SELECT Status,COUNT_BIG(*) AS Total FROM dbo.Cases GROUP BY Status ORDER BY Status");
        }
    }
}
