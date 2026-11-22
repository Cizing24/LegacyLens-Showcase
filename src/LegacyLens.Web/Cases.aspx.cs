using System;
using System.Web.UI.WebControls;
using LegacyLens.Infrastructure;

namespace LegacyLens.Web
{
    public class CasesPage : SecurePage
    {
        protected TextBox SearchTitle;
        protected DropDownList Status;
        protected GridView CasesGrid;
        protected Repeater Stats;
        protected Label Message,PageInfo;
        protected Button Previous,Next;
        private int PageNumber { get { return (int)(ViewState["PageNumber"] ?? 0); } set { ViewState["PageNumber"] = value; } }
        protected void Page_Load(object sender, EventArgs e) { if (!IsPostBack) Bind(); }
        private void Bind()
        {
            Attempt(() => {
                var store = new CaseStore(Db);
                var table = store.Search(SearchTitle.Text,Status.SelectedValue,PageNumber);
                Next.Enabled = table.Rows.Count > 20;
                if (table.Rows.Count > 20) table.Rows.RemoveAt(20);
                CasesGrid.DataSource = table;CasesGrid.DataBind();
                Previous.Enabled = PageNumber > 0;
                PageInfo.Text = "第 " + (PageNumber + 1) + " 頁 · 每頁 20 筆";
                Stats.DataSource = store.Statistics();Stats.DataBind();
            },Message);
        }
        public static string StatusLabel(string status) { return status == "Open" ? "待處理" : status == "Closed" ? "已結案" : "處理中"; }
        protected void Search_Click(object sender, EventArgs e) { PageNumber = 0;Bind(); }
        protected void Previous_Click(object sender, EventArgs e) { PageNumber = Math.Max(0,PageNumber - 1);Bind(); }
        protected void Next_Click(object sender, EventArgs e) { if (PageNumber < 50000) PageNumber++;Bind(); }
    }
}
