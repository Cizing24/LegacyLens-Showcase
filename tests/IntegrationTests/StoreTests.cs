using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using LegacyLens.Application;
using LegacyLens.Domain;
using LegacyLens.Infrastructure;
using NUnit.Framework;

namespace LegacyLens.Tests
{
    public abstract class DatabaseFixture
    {
        protected Database db;
        protected Actor manager,agent,other;
        protected int customerId;
        [OneTimeSetUp]
        public void ConnectToIsolatedDatabase()
        {
            db = new Database("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=LegacyLensShowcaseTests;Integrated Security=True");
            var identities = new IdentityStore(db);
            manager = identities.Find("manager");agent = identities.Find("agent");other = identities.Find("agent2");
            Assert.That(manager,Is.Not.Null,"Run database setup and seed on LegacyLensShowcaseTests first.");
            customerId = (int)db.Query("SELECT MIN(Id) AS Id FROM dbo.Customers").Rows[0]["Id"];
        }
        protected CaseRecord Create()
        {
            var item = new CaseRecord { CustomerId=customerId,Title="Integration "+Guid.NewGuid(),Status="Open",AssignedTo=agent.Id,Description="synthetic test" };
            var store = new CaseStore(db);
            new CaseService(store).Save(item,manager,Guid.NewGuid().ToString());
            return store.Get(item.Id);
        }
    }
    [TestFixture, Category("Integration"), NonParallelizable]
    public class StoreTests : DatabaseFixture
    {
        [Test] public void CreatePersistsHistoryAndAudit()
        {
            var item = Create();
            Assert.That(new CaseStore(db).History(item.Id).Rows.Count,Is.EqualTo(1));
            Assert.That(db.Query("SELECT Id FROM dbo.AuditLog WHERE EntityId=@id AND Action='Created'",Database.P("@id",SqlDbType.Int,item.Id)).Rows.Count,Is.EqualTo(1));
        }
        [Test] public void UpdatePreservesReadAndRecordsTransition()
        {
            var item = Create();item.Status="InProgress";
            new CaseService(new CaseStore(db)).Save(item,agent,Guid.NewGuid().ToString());
            Assert.That(new CaseStore(db).Get(item.Id).Status,Is.EqualTo("InProgress"));
            Assert.That(new CaseStore(db).History(item.Id).Rows.Count,Is.EqualTo(2));
        }
        [Test] public void StaleUpdateCannotOverwriteOrAppendHistory()
        {
            var item=Create();var stale=new CaseStore(db).Get(item.Id);item.Title="Fresh "+Guid.NewGuid();
            var service=new CaseService(new CaseStore(db));service.Save(item,manager,Guid.NewGuid().ToString());
            stale.Title="Must never overwrite";
            Assert.Throws<ConflictException>(() => service.Save(stale,manager,Guid.NewGuid().ToString()));
            Assert.That(new CaseStore(db).Get(item.Id).Title,Is.EqualTo(item.Title));
            Assert.That(new CaseStore(db).History(item.Id).Rows.Count,Is.EqualTo(2));
        }
        [Test] public void ConcurrentUpdatesHaveExactlyOneWinner()
        {
            var item=Create();var stale=new CaseStore(db).Get(item.Id);
            var results=new[] {item,stale}.Select(record => Task.Run(() => {
                try { new CaseService(new CaseStore(db)).Save(record,manager,Guid.NewGuid().ToString());return "saved"; }
                catch(ConflictException) { return "conflict"; }
            })).ToArray();
            Task.WaitAll(results);
            Assert.That(results.Count(task=>task.Result=="saved"),Is.EqualTo(1));
            Assert.That(results.Count(task=>task.Result=="conflict"),Is.EqualTo(1));
        }
        [Test] public void AuditFailureRollsBackCaseInsert()
        {
            var title="Rollback "+Guid.NewGuid();
            var item=new CaseRecord {CustomerId=customerId,Title=title,Status="Open",AssignedTo=agent.Id};
            Assert.Throws<SqlException>(()=>new CaseService(new CaseStore(db)).Save(item,new Actor {Id=int.MaxValue,Role="Manager"},Guid.NewGuid().ToString()));
            Assert.That(new CaseStore(db).Search(title,"",0).Rows.Count,Is.Zero);
        }
        [Test] public void ForeignKeyFailureDoesNotCreateHistory()
        {
            var item=new CaseRecord {CustomerId=int.MaxValue,Title="Invalid customer",Status="Open",AssignedTo=agent.Id};
            Assert.Throws<SqlException>(()=>new CaseService(new CaseStore(db)).Save(item,manager,Guid.NewGuid().ToString()));
        }
        [Test] public void InjectionTextIsAParameter()
        {
            Assert.That(new CaseStore(db).Search("' OR 1=1 --","",0).Rows.Count,Is.Zero);
        }
        [Test] public void CustomerServiceTrimsAndPersistsValidCustomer()
        {
            var customer = new Customer { Name="  合成測試客戶 "+Guid.NewGuid()+"  ",Industry="  測試產業  " };
            var id = new CustomerService(new CustomerStore(db)).Save(customer,manager,Guid.NewGuid().ToString());
            var saved = new CustomerStore(db).Get(id);
            Assert.That(saved.Name,Is.EqualTo(customer.Name.Trim()));
            Assert.That(saved.Industry,Is.EqualTo("測試產業"));
        }
        [Test] public void ViewerCannotWriteCustomer()
        {
            Assert.Throws<UnauthorizedAccessException>(()=>new CustomerStore(db).Save(new Customer {Name="Denied",Industry="Test"},new Actor {Id=manager.Id,Role="Viewer"},Guid.NewGuid().ToString()));
        }
        [Test] public void InactiveAssigneeCannotReceiveCases()
        {
            var item=Create();item.AssignedTo=int.MaxValue;
            Assert.Throws<ArgumentException>(()=>new CaseService(new CaseStore(db)).Save(item,manager,Guid.NewGuid().ToString()));
        }
        [Test] public void SchemaReapplicationPreservesExistingRows()
        {
            var item=Create();
            var root=System.IO.Path.GetFullPath(System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory,"..","..","..",".."));
            using(var connection=db.Open())
            using(var command=new SqlCommand(System.IO.File.ReadAllText(System.IO.Path.Combine(root,"database","schema","001-baseline.sql")),connection)) command.ExecuteNonQuery();
            Assert.That(new CaseStore(db).Get(item.Id).Title,Is.EqualTo(item.Title));
        }
    }
}
