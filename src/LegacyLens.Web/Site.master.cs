using System;
using System.Web.Security;
using System.Web.UI;

namespace LegacyLens.Web
{
    public class SiteMaster : MasterPage
    {
        protected SecurePage Current { get { return (SecurePage)Page; } }
        protected void Logout_Click(object sender, EventArgs e)
        {
            Global.Log(Context,"Logout");
            FormsAuthentication.SignOut();Session.Clear();Session.Abandon();
            Response.Redirect("~/Login.aspx",true);
        }
    }
}
