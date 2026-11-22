using System;
using LegacyLens.Application;
using LegacyLens.Domain;
using LegacyLens.Infrastructure;
using NUnit.Framework;

namespace LegacyLens.Tests
{
    [TestFixture]
    public class RuleTests
    {
        private readonly Actor manager = new Actor { Id = 1, Role = "Manager" };
        private readonly Actor agent = new Actor { Id = 2, Role = "Agent" };
        private static CaseRecord NewCase() { return new CaseRecord { Title = "測試案件",CustomerId = 1,AssignedTo = 2,Status = "Open" }; }
        [Test] public void ManagerCanCreateAssignedCase() { Assert.DoesNotThrow(() => CaseRules.Validate(NewCase(),manager,null)); }
        [Test] public void ViewerCannotWrite() { Assert.Throws<UnauthorizedAccessException>(() => CaseRules.Validate(NewCase(),new Actor { Role = "Viewer" },null)); }
        [Test] public void AnonymousCannotWrite() { Assert.Throws<UnauthorizedAccessException>(() => CaseRules.Validate(NewCase(),null,null)); }
        [Test] public void AgentCannotAssignSomeoneElse() { var item = NewCase();item.AssignedTo = 1;Assert.Throws<UnauthorizedAccessException>(() => CaseRules.Validate(item,agent,null)); }
        [Test] public void AgentCannotEditOthersCase() { var old = NewCase();old.AssignedTo = 1;Assert.Throws<UnauthorizedAccessException>(() => CaseRules.Validate(NewCase(),agent,old)); }
        [Test] public void AgentCannotReopenClosedCase() { var old = NewCase();old.Status = "Closed";Assert.Throws<UnauthorizedAccessException>(() => CaseRules.Validate(NewCase(),agent,old)); }
        [Test] public void ManagerCanReopenClosedCase() { var old = NewCase();old.Status = "Closed";Assert.DoesNotThrow(() => CaseRules.Validate(NewCase(),manager,old)); }
        [TestCase("")] [TestCase(" ")] [TestCase(null)] public void EmptyTitleIsRejected(string title) { var item = NewCase();item.Title = title;Assert.Throws<ArgumentException>(() => CaseRules.Validate(item,manager,null)); }
        [Test] public void OversizedTitleIsRejected() { var item = NewCase();item.Title = new string('x',161);Assert.Throws<ArgumentException>(() => CaseRules.Validate(item,manager,null)); }
        [Test] public void OversizedDescriptionIsRejected() { var item = NewCase();item.Description = new string('x',2001);Assert.Throws<ArgumentException>(() => CaseRules.Validate(item,manager,null)); }
        [Test] public void InvalidStatusIsRejected() { var item = NewCase();item.Status = "deleted";Assert.Throws<ArgumentException>(() => CaseRules.Validate(item,manager,null)); }
        [Test] public void NewCaseMustStartOpen() { var item = NewCase();item.Status = "Closed";Assert.Throws<ArgumentException>(() => CaseRules.Validate(item,manager,null)); }
        [Test] public void MissingCustomerIsRejected() { var item = NewCase();item.CustomerId = 0;Assert.Throws<ArgumentException>(() => CaseRules.Validate(item,manager,null)); }
        [Test] public void MissingAssigneeIsRejected() { var item = NewCase();item.AssignedTo = 0;Assert.Throws<ArgumentException>(() => CaseRules.Validate(item,manager,null)); }
        [Test] public void CustomerNameAndIndustryAreRequired()
        {
            Assert.Throws<ArgumentException>(() => CustomerRules.Validate(new Customer { Name=" ", Industry="合成產業" },manager));
            Assert.Throws<ArgumentException>(() => CustomerRules.Validate(new Customer { Name="合成客戶", Industry="" },manager));
        }
        [Test] public void CustomerLengthLimitsAreEnforced()
        {
            Assert.Throws<ArgumentException>(() => CustomerRules.Validate(new Customer { Name=new string('x',121), Industry="產業" },manager));
            Assert.Throws<ArgumentException>(() => CustomerRules.Validate(new Customer { Name="客戶", Industry=new string('x',81) },manager));
        }
        [Test] public void CustomerRuleRejectsUnauthorizedActorAndNull()
        {
            var customer=new Customer { Name="合成客戶",Industry="產業" };
            Assert.Throws<UnauthorizedAccessException>(() => CustomerRules.Validate(customer,agent));
            Assert.Throws<UnauthorizedAccessException>(() => CustomerRules.Validate(customer,null));
            Assert.Throws<ArgumentException>(() => CustomerRules.Validate(null,manager));
        }
        [Test] public void PasswordHashRoundTripAndWrongPassword() { var hash = Passwords.Hash("TestingPassword!2026");Assert.That(Passwords.Verify("TestingPassword!2026",hash),Is.True);Assert.That(Passwords.Verify("IncorrectPassword!",hash),Is.False); }
        [Test] public void PasswordSaltsAreUnique() { Assert.That(Passwords.Hash("TestingPassword!2026"),Is.Not.EqualTo(Passwords.Hash("TestingPassword!2026"))); }
        [TestCase("")] [TestCase("garbage")] [TestCase("100000:bad:bad")] public void MalformedHashFailsClosed(string hash) { Assert.That(Passwords.Verify("TestingPassword!2026",hash),Is.False); }
        [Test] public void ShortPasswordRejected() { Assert.Throws<ArgumentException>(() => Passwords.Hash("short")); }
    }
}
