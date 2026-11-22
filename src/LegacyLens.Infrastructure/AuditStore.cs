using System.Data;

namespace LegacyLens.Infrastructure
{
    public sealed class AuditStore
    {
        private readonly Database db;
        public AuditStore(Database db) { this.db = db; }

        public DataTable Recent()
        {
            return db.Query(@"SELECT TOP(100) a.Action AS [操作],a.EntityId AS [資料編號],u.Username AS [操作者],
a.OccurredAt AS [時間 UTC],a.CorrelationId AS [追蹤編號] FROM dbo.AuditLog a
JOIN dbo.Users u ON a.ActorId=u.Id ORDER BY a.Id DESC");
        }
    }
}
