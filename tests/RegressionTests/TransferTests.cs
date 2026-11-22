using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using LegacyLens.Application;
using LegacyLens.Domain;
using LegacyLens.Infrastructure;
using NUnit.Framework;

namespace LegacyLens.Tests
{
    [TestFixture,Category("Integration"),NonParallelizable]
    public class TransferTests : DatabaseFixture
    {
        private TransferRequest Request(CaseRecord item)
        {
            return new TransferRequest {RequestId=Guid.NewGuid(),CaseId=item.Id,ToAssignee=other.Id,Reason="合成測試：承辦工作調整",Version=item.Version};
        }
        [Test] public void ManagerTransferChangesOwnerAndAddsReason()
        {
            var item=Create();var request=Request(item);new TransferStore(db).Transfer(request,manager,Guid.NewGuid().ToString());
            Assert.That(new CaseStore(db).Get(item.Id).AssignedTo,Is.EqualTo(other.Id));
            var history=new CaseStore(db).History(item.Id);
            Assert.That(history.Rows[0]["Reason"],Is.EqualTo(request.Reason));
            Assert.That(history.Rows.Count,Is.EqualTo(2));
        }
        [Test] public void ExactDuplicateIsIdempotent()
        {
            var item=Create();var request=Request(item);var store=new TransferStore(db);
            store.Transfer(request,manager,Guid.NewGuid().ToString());store.Transfer(request,manager,Guid.NewGuid().ToString());
            Assert.That(new CaseStore(db).History(item.Id).Rows.Count,Is.EqualTo(2));
        }
        [Test] public void ReusedKeyWithDifferentPayloadIsRejected()
        {
            var request=Request(Create());var store=new TransferStore(db);store.Transfer(request,manager,Guid.NewGuid().ToString());request.Reason="不同的轉派原因";
            Assert.Throws<ConflictException>(()=>store.Transfer(request,manager,Guid.NewGuid().ToString()));
        }
        [Test] public void AgentCannotTransfer()
        {
            Assert.Throws<UnauthorizedAccessException>(()=>new TransferStore(db).Transfer(Request(Create()),agent,Guid.NewGuid().ToString()));
        }
        [Test] public void ClosedCaseCannotTransfer()
        {
            var item=Create();item.Status="Closed";new CaseService(new CaseStore(db)).Save(item,manager,Guid.NewGuid().ToString());
            Assert.Throws<ArgumentException>(()=>new TransferStore(db).Transfer(Request(new CaseStore(db).Get(item.Id)),manager,Guid.NewGuid().ToString()));
        }
        [Test] public void SameAssigneeIsRejected()
        {
            var item=Create();var request=Request(item);request.ToAssignee=item.AssignedTo;
            Assert.Throws<ArgumentException>(()=>new TransferStore(db).Transfer(request,manager,Guid.NewGuid().ToString()));
        }
        [Test] public void ShortReasonIsRejected()
        {
            var request=Request(Create());request.Reason="短";
            Assert.Throws<ArgumentException>(()=>new TransferStore(db).Transfer(request,manager,Guid.NewGuid().ToString()));
        }
        [Test] public void ViewerCannotReceiveTransfer()
        {
            var request=Request(Create());request.ToAssignee=new IdentityStore(db).Find("viewer").Id;
            Assert.Throws<ArgumentException>(()=>new TransferStore(db).Transfer(request,manager,Guid.NewGuid().ToString()));
        }
        [Test] public void StaleTransferDoesNotAppendLedger()
        {
            var item=Create();var request=Request(item);item.Title="Updated before transfer";
            new CaseService(new CaseStore(db)).Save(item,manager,Guid.NewGuid().ToString());
            Assert.Throws<ConflictException>(()=>new TransferStore(db).Transfer(request,manager,Guid.NewGuid().ToString()));
            Assert.That(db.Query("SELECT RequestId FROM dbo.TransferOperations WHERE RequestId=@id",Database.P("@id",SqlDbType.UniqueIdentifier,request.RequestId)).Rows.Count,Is.Zero);
        }
        [Test] public void ConcurrentDuplicateHasOneLedgerEntry()
        {
            var item=Create();var request=Request(item);
            var tasks=Enumerable.Range(0,2).Select(_=>Task.Run(()=>new TransferStore(db).Transfer(request,manager,Guid.NewGuid().ToString()))).ToArray();
            Task.WaitAll(tasks);
            Assert.That(new CaseStore(db).History(item.Id).Rows.Count,Is.EqualTo(2));
        }
        [Test] public void OldSaveCannotBypassTransferReason()
        {
            var item=Create();item.AssignedTo=other.Id;
            Assert.Throws<ArgumentException>(()=>new CaseService(new CaseStore(db)).Save(item,manager,Guid.NewGuid().ToString()));
        }
        [Test] public void InactiveAgentCannotReceiveTransfer()
        {
            var request=Request(Create());
            using(var connection=db.Open())
            using(var command=connection.CreateCommand())
            {
                command.CommandText="UPDATE dbo.Users SET IsActive=0 WHERE Id=@id";command.Parameters.Add(Database.P("@id",SqlDbType.Int,other.Id));command.ExecuteNonQuery();
                try { Assert.Throws<ArgumentException>(()=>new TransferStore(db).Transfer(request,manager,Guid.NewGuid().ToString())); }
                finally { command.CommandText="UPDATE dbo.Users SET IsActive=1 WHERE Id=@id";command.ExecuteNonQuery(); }
            }
        }
    }
}
