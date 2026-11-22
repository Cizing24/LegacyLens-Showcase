using System;
using System.Web.UI.WebControls;
using LegacyLens.Application;
using LegacyLens.Domain;
using LegacyLens.Infrastructure;

namespace LegacyLens.Web
{
    public class CaseEditPage : SecurePage
    {
        protected TextBox CaseTitle,Description,TransferReason;
        protected DropDownList CustomerId,AssignedTo,Status,Category,TransferTarget;
        protected System.Web.UI.WebControls.Panel TransferPanel;
        protected Label Message,CategoryHint;
        protected Literal Heading;
        protected GridView HistoryGrid;
        protected Button Save;
        public int CaseId
        {
            get { int id;return int.TryParse(Request.QueryString["id"],out id) && id > 0 ? id : 0; }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            // Postback data is already loaded here; rebinding would discard unsaved edits (WEB-001).
            if (!IsPostBack)
            {
                Attempt(() => {
                    CustomerId.DataSource = new CustomerStore(Db).Choices();CustomerId.DataValueField = "Id";CustomerId.DataTextField = "Name";CustomerId.DataBind();
                    AssignedTo.DataSource = new IdentityStore(Db).Agents();AssignedTo.DataValueField = "Id";AssignedTo.DataTextField = "Username";AssignedTo.DataBind();
                    if (AssignedTo.Items.FindByValue(Actor.Id.ToString()) != null) AssignedTo.SelectedValue = Actor.Id.ToString();
                    if (CaseId > 0)
                    {
                        LoadCase();
                        TransferTarget.DataSource=new IdentityStore(Db).Agents();TransferTarget.DataValueField="Id";TransferTarget.DataTextField="Username";TransferTarget.DataBind();
                        ViewState["TransferRequestId"]=Guid.NewGuid();
                    }
                    else Status.Enabled = false;
                },Message);
            }
            AssignedTo.Enabled = Actor.CanManage && CaseId==0;
            TransferPanel.Visible = Actor.CanManage && CaseId>0 && new TransferStore(Db).Enabled();
            Save.Enabled = Actor.CanEdit;
        }
        private void LoadCase()
        {
            var item = new CaseStore(Db).Get(CaseId);
            Heading.Text = "編輯案件";
            CaseTitle.Text = item.Title;Description.Text = item.Description;
            CustomerId.SelectedValue = item.CustomerId.ToString();
            // Historical cases may still belong to a disabled user or a former Agent.
            // Preserve the existing read-only assignment so managers can transfer the case.
            var previousAssignee = item.AssignedTo.ToString();
            if (AssignedTo.Items.FindByValue(previousAssignee) == null)
                AssignedTo.Items.Add(new ListItem("原承辦人（已停用或角色已變更，ID " + previousAssignee + "）", previousAssignee));
            AssignedTo.SelectedValue = previousAssignee;
            Status.SelectedValue = item.Status;ViewState["Version"] = item.Version;
            BindHistory();
        }
        private void BindHistory() { HistoryGrid.DataSource = new CaseStore(Db).History(CaseId);HistoryGrid.DataBind(); }
        protected void Category_Changed(object sender, EventArgs e) { CategoryHint.Text = "已切換分類提示；尚未儲存的案件內容仍保留。";if (CaseId > 0) BindHistory(); }
        protected void Save_Click(object sender, EventArgs e)
        {
            Attempt(() => {
                var item = new CaseRecord { Id = CaseId,Title = CaseTitle.Text,Description = Description.Text,
                    CustomerId = int.Parse(CustomerId.SelectedValue),AssignedTo = int.Parse(AssignedTo.SelectedValue),
                    Status = CaseId == 0 ? "Open" : Status.SelectedValue,Version = (byte[])ViewState["Version"] };
                int id = new CaseService(new CaseStore(Db)).Save(item,Actor,CorrelationId);
                Global.Log(Context,"CaseSaved");
                Response.Redirect("CaseEdit.aspx?id=" + id + "&saved=1",false);Context.ApplicationInstance.CompleteRequest();
            },Message);
        }
        protected void Transfer_Click(object sender, EventArgs e)
        {
            Attempt(() => {
                var request=new TransferRequest {RequestId=(Guid)(ViewState["TransferRequestId"] ?? Guid.Empty),CaseId=CaseId,
                    ToAssignee=int.Parse(TransferTarget.SelectedValue),Reason=TransferReason.Text,Version=(byte[])ViewState["Version"]};
                new TransferStore(Db).Transfer(request,Actor,CorrelationId);
                Global.Log(Context,"CaseReassigned");
                Response.Redirect("CaseEdit.aspx?id="+CaseId+"&saved=1",false);Context.ApplicationInstance.CompleteRequest();
            },Message);
        }
        protected void Page_PreRender(object sender, EventArgs e)
        {
            if (!IsPostBack && Request.QueryString["saved"] == "1") Message.Text = "案件已儲存，處理歷程已更新。";
        }
    }
}
