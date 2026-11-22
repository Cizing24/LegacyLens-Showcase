using System;
using System.Web.UI.WebControls;
using LegacyLens.Infrastructure;

namespace LegacyLens.Web
{
    public class ReportsPage : SecurePage
    {
        protected GridView StatsGrid,AuditGrid;
        protected Label Message;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            StatsGrid.DataSource = new CaseStore(Db).Statistics();StatsGrid.DataBind();
            if (!Actor.CanManage) { Message.Text = "目前角色可以查看統計；稽核明細需要主管權限。";return; }
            AuditGrid.DataSource = new AuditStore(Db).Recent();
            AuditGrid.DataBind();
        }
    }
}
