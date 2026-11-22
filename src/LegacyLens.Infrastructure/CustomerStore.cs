using System;
using System.Data;
using System.Data.SqlClient;
using LegacyLens.Domain;
using LegacyLens.Application;

namespace LegacyLens.Infrastructure
{
    public sealed class CustomerStore : ICustomerStore
    {
        private readonly Database db;
        public CustomerStore(Database db) { this.db = db; }
        public DataTable Search(string name, string industry = "")
        {
            if ((name ?? "").Length > 120 || (industry ?? "").Length > 80) throw new ArgumentException("搜尋條件過長。");
            return db.Query(@"SELECT TOP(100) Id,Name,Industry,Version FROM dbo.Customers
WHERE Name LIKE N'%'+@name+N'%' AND (@industry=N'' OR Industry=@industry) ORDER BY Id",
                Database.P("@name",SqlDbType.NVarChar,name ?? "",120),Database.P("@industry",SqlDbType.NVarChar,industry ?? "",80));
        }
        public Customer Get(int id)
        {
            var rows = db.Query("SELECT Id,Name,Industry,Version FROM dbo.Customers WHERE Id=@id",
                Database.P("@id", SqlDbType.Int, id));
            if (rows.Rows.Count == 0) throw new ArgumentException("找不到客戶。");
            var row = rows.Rows[0];
            return new Customer { Id = (int)row["Id"], Name = (string)row["Name"],
                Industry = (string)row["Industry"], Version = (byte[])row["Version"] };
        }
        public DataTable Choices()
        {
            return db.Query("SELECT Id,Name FROM dbo.Customers ORDER BY Name");
        }
        public int Save(Customer customer, Actor actor, string correlationId)
        {
            if (actor == null || !actor.CanManage) throw new UnauthorizedAccessException();
            using (var connection = db.Open())
            using (var tx = connection.BeginTransaction())
            using (var command = new SqlCommand(customer.Id == 0
                ? "INSERT dbo.Customers(Name,Industry) OUTPUT INSERTED.Id VALUES(@name,@industry)"
                : "UPDATE dbo.Customers SET Name=@name,Industry=@industry OUTPUT INSERTED.Id WHERE Id=@id AND Version=@version", connection, tx))
            {
                command.Parameters.AddRange(new[] {
                    Database.P("@name",SqlDbType.NVarChar,customer.Name.Trim(),120), Database.P("@industry",SqlDbType.NVarChar,customer.Industry.Trim(),80),
                    Database.P("@id",SqlDbType.Int,customer.Id), Database.P("@version",SqlDbType.Binary,customer.Version,8)
                });
                var result = command.ExecuteScalar();
                if (result == null) throw new ConflictException();
                customer.Id = (int)result;
                using (var audit = new SqlCommand("INSERT dbo.AuditLog(ActorId,Action,EntityId,CorrelationId) VALUES(@actor,'CustomerSaved',@id,@correlation)",connection,tx))
                {
                    audit.Parameters.AddRange(new[] { Database.P("@actor",SqlDbType.Int,actor.Id),Database.P("@id",SqlDbType.Int,customer.Id),Database.P("@correlation",SqlDbType.VarChar,correlationId,36) });
                    audit.ExecuteNonQuery();
                }
                tx.Commit();
                return customer.Id;
            }
        }
    }
}
