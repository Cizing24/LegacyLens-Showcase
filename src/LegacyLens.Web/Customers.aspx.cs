using System;
using System.Web.UI.WebControls;
using LegacyLens.Domain;
using LegacyLens.Application;
using LegacyLens.Infrastructure;

namespace LegacyLens.Web
{
    public class CustomersPage : SecurePage
    {
        protected TextBox Keyword,FilterIndustry,CustomerName,Industry;
        protected Label Message;
        protected GridView CustomersGrid;
        protected Literal FormHeading;
        protected Button Save;
        private int Id { get { int id;return int.TryParse(Request.QueryString["id"],out id) && id > 0 ? id : 0; } }
        protected void Page_Load(object sender, EventArgs e)
        {
            Save.Enabled = Actor.CanManage;
            if (!IsPostBack) Attempt(() => {
                Bind();
                if (Id > 0)
                {
                    var customer = new CustomerStore(Db).Get(Id);
                    CustomerName.Text = customer.Name;Industry.Text = customer.Industry;
                    ViewState["Version"] = customer.Version;FormHeading.Text = "編輯客戶";
                }
                if (Request.QueryString["saved"] == "1") Message.Text = "客戶資料已儲存。";
            },Message);
        }
        private void Bind() { CustomersGrid.DataSource = new CustomerStore(Db).Search(Keyword.Text,FilterIndustry.Text);CustomersGrid.DataBind(); }
        protected void Search_Click(object sender, EventArgs e) { Attempt(Bind,Message); }
        protected void Save_Click(object sender, EventArgs e)
        {
            Attempt(() => {
                int id = new CustomerService(new CustomerStore(Db)).Save(new Customer { Id = Id,Name = CustomerName.Text,Industry = Industry.Text,Version = (byte[])ViewState["Version"] },Actor,CorrelationId);
                Response.Redirect("Customers.aspx?id=" + id + "&saved=1",false);Context.ApplicationInstance.CompleteRequest();
            },Message);
        }
    }
}
