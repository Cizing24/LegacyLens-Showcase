using System;
using System.Data;
using System.Data.SqlClient;
using LegacyLens.Application;
using LegacyLens.Domain;

namespace LegacyLens.Infrastructure
{
    public sealed class TransferStore
    {
        private readonly Database db;
        public TransferStore(Database db) { this.db = db; }
        public bool Enabled()
        {
            var rows=db.Query("SELECT Enabled FROM dbo.FeatureFlags WHERE Name='Reassignment'").Rows;
            return rows.Count==1 && (bool)rows[0]["Enabled"];
        }
        public void Transfer(TransferRequest request,Actor actor,string correlationId)
        {
            if (actor == null || !actor.CanManage) throw new UnauthorizedAccessException("案件轉派需要主管權限。");
            if (request == null || request.RequestId == Guid.Empty) throw new ArgumentException("轉派要求無效。");
            using (var connection=db.Open())
            using (var tx=connection.BeginTransaction())
            {
                using (var feature=new SqlCommand("SELECT Enabled FROM dbo.FeatureFlags WITH(HOLDLOCK) WHERE Name='Reassignment'",connection,tx))
                    if (!Equals(feature.ExecuteScalar(),true)) throw new ArgumentException("案件轉派目前暫停，既有案件仍可查詢及編輯。");
                using (var duplicate=new SqlCommand("SELECT CaseId,ToAssignee,ActorId,Reason FROM dbo.TransferOperations WITH(UPDLOCK,HOLDLOCK) WHERE RequestId=@request",connection,tx))
                {
                    duplicate.Parameters.Add(Database.P("@request",SqlDbType.UniqueIdentifier,request.RequestId));
                    var rows=new DataTable();using(var reader=duplicate.ExecuteReader()) rows.Load(reader);
                    if(rows.Rows.Count>0)
                    {
                        var row=rows.Rows[0];
                        if((int)row["CaseId"]!=request.CaseId || (int)row["ToAssignee"]!=request.ToAssignee || (int)row["ActorId"]!=actor.Id || (string)row["Reason"]!=(request.Reason ?? "").Trim())
                            throw new ConflictException();
                        tx.Commit();return;
                    }
                }
                CaseRecord current;
                using(var read=new SqlCommand("SELECT * FROM dbo.Cases WITH(UPDLOCK,HOLDLOCK) WHERE Id=@id",connection,tx))
                {
                    read.Parameters.Add(Database.P("@id",SqlDbType.Int,request.CaseId));
                    var rows=new DataTable();using(var reader=read.ExecuteReader()) rows.Load(reader);
                    current=rows.Rows.Count==0?null:CaseStore.Read(rows.Rows[0]);
                }
                TransferRules.Validate(request,actor,current);
                using(var eligible=new SqlCommand("SELECT COUNT(*) FROM dbo.Users WITH(HOLDLOCK) WHERE Id=@target AND IsActive=1 AND Role IN ('Manager','Agent')",connection,tx))
                {
                    eligible.Parameters.Add(Database.P("@target",SqlDbType.Int,request.ToAssignee));
                    if((int)eligible.ExecuteScalar()!=1) throw new ArgumentException("新承辦人已停用或無處理權限。");
                }
                using(var update=new SqlCommand(@"UPDATE dbo.Cases SET AssignedTo=@target,UpdatedAt=SYSUTCDATETIME(),AssignmentPolicyVersion=2 WHERE Id=@id AND Version=@version;
IF @@ROWCOUNT<>1 THROW 51001,'Stale case version',1;
INSERT dbo.TransferOperations(RequestId,CaseId,FromAssignee,ToAssignee,ActorId,Reason) VALUES(@request,@id,@old,@target,@actor,@reason);
INSERT dbo.CaseHistory(CaseId,ActorId,Action,OldStatus,NewStatus,OldAssignee,NewAssignee,CorrelationId,Reason,RequestId)
VALUES(@id,@actor,'Reassigned',@status,@status,@old,@target,@correlation,@reason,@request);
INSERT dbo.AuditLog(ActorId,Action,EntityId,CorrelationId) VALUES(@actor,'Reassigned',@id,@correlation)",connection,tx))
                {
                    update.Parameters.AddRange(new[] {
                        Database.P("@target",SqlDbType.Int,request.ToAssignee),Database.P("@id",SqlDbType.Int,request.CaseId),Database.P("@version",SqlDbType.Binary,request.Version,8),
                        Database.P("@request",SqlDbType.UniqueIdentifier,request.RequestId),Database.P("@old",SqlDbType.Int,current.AssignedTo),Database.P("@actor",SqlDbType.Int,actor.Id),
                        Database.P("@reason",SqlDbType.NVarChar,request.Reason.Trim(),500),Database.P("@status",SqlDbType.VarChar,current.Status,16),Database.P("@correlation",SqlDbType.VarChar,correlationId,36)
                    });
                    try { update.ExecuteNonQuery(); }
                    catch(SqlException ex) when(ex.Number==51001) { throw new ConflictException(); }
                }
                tx.Commit();
            }
        }
    }
}
