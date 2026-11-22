using System;
using System.Data;
using System.Linq;
using LegacyLens.Infrastructure;
using NUnit.Framework;

namespace LegacyLens.Tests
{
    [TestFixture,Category("Integration")]
    public class SearchTests
    {
        private readonly Database db = new Database("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=LegacyLensShowcaseTests;Integrated Security=True");
        [TestCase("", "", 0)] [TestCase("", "Open", 0)] [TestCase("Integration", "Open", 0)]
        [TestCase("", "Closed", 0)] [TestCase("", "", 1)] [TestCase("absent-nonexistent-keyword", "", 0)]
        public void OptimizedSearchMatchesBaseline(string title,string status,int page)
        {
            var old=db.Query(@"SELECT c.Id FROM dbo.Cases c JOIN dbo.Users u ON c.AssignedTo=u.Id JOIN dbo.Customers k ON c.CustomerId=k.Id
WHERE (@title=N'' OR c.Title LIKE N'%'+@title+N'%') AND (@status='' OR c.Status=@status)
ORDER BY c.CreatedAt DESC,c.Id DESC OFFSET @offset ROWS FETCH NEXT 21 ROWS ONLY",
                Database.P("@title",SqlDbType.NVarChar,title,160),Database.P("@status",SqlDbType.VarChar,status,16),Database.P("@offset",SqlDbType.Int,page*20));
            var actual=new CaseStore(db).Search(title,status,page);
            Assert.That(actual.Rows.Cast<DataRow>().Select(row=>(int)row["Id"]),Is.EqualTo(old.Rows.Cast<DataRow>().Select(row=>(int)row["Id"])));
        }
        [Test] public void InvalidFilterIsRejected() { Assert.Throws<ArgumentException>(()=>new CaseStore(db).Search("","invalid",0)); }
        [Test] public void NegativePageIsRejected() { Assert.Throws<ArgumentException>(()=>new CaseStore(db).Search("","",-1)); }
    }
}
